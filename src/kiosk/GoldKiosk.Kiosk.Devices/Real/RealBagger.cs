using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real bagger driver. The bagging unit has no controller of its own — the legacy GXBPS path
/// ("get chamber, put bag") drove it entirely through arm choreography, with
/// <c>BAGGER_GET_STATUS</c> gating new transactions while a bag cycle ran. This driver
/// delegates to the injected <see cref="IRoboticArm"/> (<see cref="ArmMove.ChamberToBag"/>
/// then <see cref="ArmMove.Home"/>) and exposes the same status interlock. Without an
/// injected arm it faults with <c>dependency_missing:robotic_arm</c> at connect (the
/// parameterless composition constructor is wired up in a Kiosk.Api follow-up).
/// </summary>
public sealed class RealBagger : RealDeviceBase, IBagger
{
    private readonly IRoboticArm? _roboticArm;
    private volatile BaggerStatus _status = BaggerStatus.Idle;

    /// <summary>Initializes the driver without an arm (faults visibly at connect until composition wires one).</summary>
    public RealBagger()
        : this(null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="roboticArm">The arm executing the bagging choreography.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealBagger(IRoboticArm? roboticArm, TimeProvider? timeProvider = null)
        : base(DeviceKeys.Bagger, "Bagger (arm-driven)", isCritical: true, timeProvider)
    {
        _roboticArm = roboticArm;
    }

    /// <inheritdoc />
    public Task<BaggerStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        BaggerStatus status = Health.State == DeviceState.Faulted ? BaggerStatus.Faulted : _status;
        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public async Task BagItemAsync(string bagNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bagNumber);
        EnsureOperable();
        IRoboticArm arm = _roboticArm ?? throw new InvalidOperationException("bagger_arm_missing");
        if (_status == BaggerStatus.Bagging)
        {
            throw new InvalidOperationException("bagger_cycle_in_progress");
        }

        _status = BaggerStatus.Bagging;
        SetHealth(DeviceState.Busy, "Bagging item.");
        try
        {
            await arm.MoveAsync(ArmMove.ChamberToBag, cancellationToken).ConfigureAwait(false);
            await arm.MoveAsync(ArmMove.Home, cancellationToken).ConfigureAwait(false);
            _status = BaggerStatus.Idle;
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            _status = BaggerStatus.Idle;
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            _status = BaggerStatus.Faulted;
            SetHealth(DeviceState.Faulted, $"bagging_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        _roboticArm is null
            ? throw new DeviceConnectFailedException("dependency_missing:robotic_arm")
            : Task.CompletedTask;

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    protected override Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_roboticArm is null)
        {
            return Task.FromResult(new DeviceProbeResult(false, 0, "dependency_missing:robotic_arm"));
        }

        DeviceHealth armHealth = _roboticArm.Health;
        return Task.FromResult(armHealth.State is DeviceState.Ready or DeviceState.Busy
            ? new DeviceProbeResult(true, 0, $"Bagger {_status}; arm {armHealth.State}.")
            : new DeviceProbeResult(false, 0, $"bagger_arm_unavailable:{armHealth.State}"));
    }
}
