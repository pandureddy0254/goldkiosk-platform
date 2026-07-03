using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using GoldKiosk.Kiosk.Devices.Real.Transport;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real volume chamber driver: Boyle's-law two-pressure measurement using the shared stepper
/// board channel (COM3: <c>cu</c> seal / <c>pu</c> piston up / <c>pd</c> piston down /
/// <c>cd</c> open, <c>*</c> acks) and the pressure sensor (COM4, <c>P</c> command). The math
/// is the verbatim legacy production path (<see cref="BoylesLawVolume"/>); constants and the
/// config-vs-code discrepancies are documented on <see cref="VolumeChamberOptions"/>.
/// Sampling preserves legacy semantics: unparseable pressure lines contribute zero and the
/// average divides by the full sample count.
/// </summary>
/// <remarks>
/// The <c>weightGrams</c> parameter of <see cref="MeasureAsync"/> is accepted for the port
/// contract but not consumed here — the density cross-check (weight ÷ volume vs XRF
/// composition) is computed upstream in the transaction flow, matching the legacy split
/// between the hardware sidecar and <c>GCKaratCalculator</c>.
/// </remarks>
public sealed class RealVolumeChamber : RealDeviceBase, IVolumeChamber
{
    private readonly ConnectionOptions _stepperConnection;
    private readonly VolumeChamberOptions _options;
    private SerialPortLease? _stepperLease;
    private SerialPortLease? _pressureLease;

    /// <summary>Initializes the driver with legacy-parity defaults (stepper COM3, pressure COM4).</summary>
    public RealVolumeChamber()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="stepperConnection">Stepper board connection overrides; <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Chamber calibration and timing; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealVolumeChamber(
        ConnectionOptions? stepperConnection,
        VolumeChamberOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.VolumeChamber, "Volume chamber (pressure + stepper)", isCritical: true, timeProvider)
    {
        _stepperConnection = (stepperConnection ?? new ConnectionOptions())
            .MergedWith(RealDeviceDefaults.For(DeviceKeys.VolumeChamber));
        _options = options ?? new VolumeChamberOptions();
    }

    /// <inheritdoc />
    public async Task<VolumeReading> MeasureAsync(decimal weightGrams, CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Measuring volume.");
        try
        {
            await StepperCommandAsync(StepperBoardCommands.ChamberSeal, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.CommandDwellMs, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.SealSettleDelayMs, cancellationToken).ConfigureAwait(false);

            decimal sealedPressure = await SamplePressureAverageAsync(cancellationToken).ConfigureAwait(false)
                + _options.AmbientPressurePsi;

            await StepperCommandAsync(StepperBoardCommands.PistonUp, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.CommandDwellMs, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.PistonSettleDelayMs, cancellationToken).ConfigureAwait(false);

            decimal compressedPressure = await SamplePressureAverageAsync(cancellationToken).ConfigureAwait(false)
                + _options.AmbientPressurePsi;

            await StepperCommandAsync(StepperBoardCommands.PistonDown, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.CommandDwellMs, cancellationToken).ConfigureAwait(false);

            decimal volumeCc = BoylesLawVolume.ComputeItemVolumeCc(sealedPressure, compressedPressure, _options);
            bool calibrated = BoylesLawVolume.IsWithinCalibration(sealedPressure, compressedPressure, _options);

            await StepperCommandAsync(StepperBoardCommands.ChamberOpen, cancellationToken).ConfigureAwait(false);
            await DwellAsync(_options.CommandDwellMs, cancellationToken).ConfigureAwait(false);

            SetHealth(DeviceState.Ready);
            return new VolumeReading(Math.Round(volumeCc, 4), calibrated);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"volume_measure_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        SerialPortLease stepper = SerialPortChannelPool.Acquire(
            _stepperConnection.Port!, _stepperConnection.BaudRate!.Value);
        SerialPortLease? pressure = null;
        try
        {
            await stepper.Channel.OpenAsync(cancellationToken).ConfigureAwait(false);
            pressure = SerialPortChannelPool.Acquire(_options.PressurePort, _options.PressureBaudRate);
            await pressure.Channel.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            pressure?.Dispose();
            stepper.Dispose();
            throw;
        }

        _stepperLease = stepper;
        _pressureLease = pressure;
    }

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseLeases();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_pressureLease is null)
        {
            return new DeviceProbeResult(false, 0, Health.Detail ?? "Volume chamber is not connected.");
        }

        // Non-destructive: a single pressure sample proves both serial links without moving
        // the chamber mechanics.
        decimal sample = await SamplePressureOnceAsync(cancellationToken).ConfigureAwait(false);
        return sample >= 0m
            ? new DeviceProbeResult(true, 0, "Pressure sensor responded.")
            : new DeviceProbeResult(false, 0, "pressure_sensor_unparseable");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseLeases();
        }

        base.Dispose(disposing);
    }

    private void ReleaseLeases()
    {
        _pressureLease?.Dispose();
        _pressureLease = null;
        _stepperLease?.Dispose();
        _stepperLease = null;
    }

    private Task DwellAsync(int milliseconds, CancellationToken cancellationToken) =>
        milliseconds <= 0
            ? Task.CompletedTask
            : Task.Delay(TimeSpan.FromMilliseconds(milliseconds), TimeProvider, cancellationToken);

    private Task<bool> StepperCommandAsync(string command, CancellationToken cancellationToken)
    {
        SerialPortChannel channel =
            (_stepperLease ?? throw new InvalidOperationException("chamber_not_connected")).Channel;
        return channel.ExecuteAsync(port =>
        {
            port.DiscardInBuffer();
            port.WriteLine(command);
            SerialPortChannel.WaitForAck(port, StepperBoardCommands.Ack, _options.AckTimeoutMs, TimeProvider);
            return true;
        }, cancellationToken);
    }

    private Task<decimal> SamplePressureAverageAsync(CancellationToken cancellationToken)
    {
        SerialPortChannel channel =
            (_pressureLease ?? throw new InvalidOperationException("chamber_not_connected")).Channel;
        int sampleCount = _options.SampleCount;
        return channel.ExecuteAsync(port =>
        {
            port.ReadTimeout = _options.PressureReadTimeoutMs;
            decimal sum = 0m;
            for (int i = 0; i < sampleCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                port.DiscardInBuffer();
                port.Write("P\r\n");
                string line;
                try
                {
                    line = port.ReadLine();
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException($"pressure_read_timeout:{port.PortName}");
                }

                // Legacy parity: a line that does not parse contributes 0 and still counts
                // toward the divisor (legacy SamplePressure returned 0m on bad frames).
                if (PressureLineParser.TryParse(line, out decimal pressure))
                {
                    sum += pressure;
                }
            }

            return sum / sampleCount;
        }, cancellationToken);
    }

    private Task<decimal> SamplePressureOnceAsync(CancellationToken cancellationToken)
    {
        SerialPortChannel channel =
            (_pressureLease ?? throw new InvalidOperationException("chamber_not_connected")).Channel;
        return channel.ExecuteAsync(port =>
        {
            port.ReadTimeout = _options.PressureReadTimeoutMs;
            port.DiscardInBuffer();
            port.Write("P\r\n");
            string line = port.ReadLine();
            return PressureLineParser.TryParse(line, out decimal pressure) ? pressure : -1m;
        }, cancellationToken);
    }
}
