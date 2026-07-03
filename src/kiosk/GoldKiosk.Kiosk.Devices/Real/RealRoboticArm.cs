using System.Globalization;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using GoldKiosk.Kiosk.Devices.Real.Transport;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real robotic arm driver: Dobot CR over TCP (legacy 192.168.1.6 — dashboard :29999 for
/// <c>ClearError()</c>/<c>EnableRobot()</c>, motion :30003 for <c>MovJ(x,y,z,r,0,0)</c>,
/// feedback :30004 for the robot-mode stream) plus the optional Kollmorgen AKD linear axis
/// (telnet ASCII) for analyser positioning. Every <see cref="MoveAsync"/> waypoint waits for
/// motion completion via the feedback stream (<see cref="DobotFeedbackMonitor"/>) — the
/// legacy blind <c>Task.Delay(500)</c> after <c>MovJ</c> is not carried forward. Named moves
/// expand to via-point sequences derived from the legacy <c>DobotHelper</c> paths
/// (home → mid-safe point → station and back).
/// </summary>
public sealed class RealRoboticArm : RealDeviceBase, IRoboticArm
{
    private const int DashboardReplyTimeoutMs = 20_000;
    private const int MotionReplyTimeoutMs = 5000;

    private readonly ConnectionOptions _connection;
    private readonly RoboticArmOptions _options;
    private DobotAsciiClient? _dashboard;
    private DobotAsciiClient? _motion;
    private DobotFeedbackMonitor? _feedback;
    private AkdAxisClient? _akd;
    private IReadOnlyDictionary<ArmMove, IReadOnlyList<ArmWaypoint>>? _sequences;

    /// <summary>Initializes the driver with legacy-parity defaults (192.168.1.6, legacy point table).</summary>
    public RealRoboticArm()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides; <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Waypoints and motion tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealRoboticArm(
        ConnectionOptions? connection,
        RoboticArmOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.RoboticArm, "Robotic arm (Dobot CR + AKD axis)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.RoboticArm));
        _options = options ?? new RoboticArmOptions();
    }

    /// <inheritdoc />
    public async Task MoveAsync(ArmMove move, CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        DobotAsciiClient motion = _motion ?? throw new InvalidOperationException("arm_not_connected");
        DobotFeedbackMonitor feedback = _feedback ?? throw new InvalidOperationException("arm_not_connected");
        IReadOnlyList<ArmWaypoint> sequence = (_sequences ?? throw new InvalidOperationException("arm_not_connected"))
            .TryGetValue(move, out IReadOnlyList<ArmWaypoint>? found)
                ? found
                : throw new InvalidOperationException($"arm_move_unmapped:{move}");

        SetHealth(DeviceState.Busy, $"Arm move {move}.");
        try
        {
            foreach (ArmWaypoint waypoint in sequence)
            {
                string command = string.Create(
                    CultureInfo.InvariantCulture,
                    $"MovJ({waypoint.X},{waypoint.Y},{waypoint.Z},{waypoint.R},0,0)");
                string reply = await motion.SendAsync(command, MotionReplyTimeoutMs, cancellationToken)
                    .ConfigureAwait(false);
                int errorId = DobotAsciiClient.ParseErrorId(reply);
                if (errorId != 0)
                {
                    throw new InvalidOperationException($"arm_movj_rejected:{errorId}");
                }

                await feedback.WaitForMotionCompleteAsync(
                        _options.MotionStartGraceMs,
                        _options.MotionTimeoutMs,
                        _options.FeedbackPollIntervalMs,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await RunAxisForMoveAsync(move, cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"arm_move_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        _sequences = BuildSequences(_options);
        string host = _connection.Host!;

        var feedback = new DobotFeedbackMonitor(TimeProvider);
        var dashboard = new DobotAsciiClient();
        var motion = new DobotAsciiClient();
        AkdAxisClient? akd = null;
        try
        {
            await feedback.ConnectAsync(host, RealDeviceDefaults.DobotFeedbackPort, cancellationToken)
                .ConfigureAwait(false);
            await dashboard.ConnectAsync(host, RealDeviceDefaults.DobotDashboardPort, cancellationToken)
                .ConfigureAwait(false);
            await motion.ConnectAsync(host, RealDeviceDefaults.DobotMotionPort, cancellationToken)
                .ConfigureAwait(false);

            await dashboard.SendAsync("ClearError()", DashboardReplyTimeoutMs, cancellationToken)
                .ConfigureAwait(false);
            string enableReply = await dashboard.SendAsync("EnableRobot()", DashboardReplyTimeoutMs, cancellationToken)
                .ConfigureAwait(false);
            if (DobotAsciiClient.ParseErrorId(enableReply) != 0)
            {
                throw new InvalidOperationException($"arm_enable_rejected:{enableReply}");
            }

            if (_options.AkdEnabled)
            {
                akd = new AkdAxisClient(TimeProvider);
                await akd.ConnectAsync(_options.AkdHost, _options.AkdPort, cancellationToken).ConfigureAwait(false);
                await akd.EnableAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            akd?.Dispose();
            motion.Dispose();
            dashboard.Dispose();
            feedback.Dispose();
            throw;
        }

        _feedback = feedback;
        _dashboard = dashboard;
        _motion = motion;
        _akd = akd;
    }

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseClients();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_dashboard is null)
        {
            return new DeviceProbeResult(false, 0, Health.Detail ?? "Arm is not connected.");
        }

        string reply = await _dashboard.SendAsync("RobotMode()", MotionReplyTimeoutMs, cancellationToken)
            .ConfigureAwait(false);
        return DobotAsciiClient.ParseErrorId(reply) == 0
            ? new DeviceProbeResult(true, 0, $"Dashboard responded; feedback mode {_feedback?.CurrentMode}.")
            : new DeviceProbeResult(false, 0, $"arm_dashboard_error:{reply}");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseClients();
        }

        base.Dispose(disposing);
    }

    private static Dictionary<ArmMove, IReadOnlyList<ArmWaypoint>> BuildSequences(RoboticArmOptions options)
    {
        ArmWaypoint home = ArmWaypoint.Parse(options.HomePoint, nameof(options.HomePoint));
        ArmWaypoint scale = ArmWaypoint.Parse(options.ScalePoint, nameof(options.ScalePoint));
        ArmWaypoint xray = ArmWaypoint.Parse(options.XrayPoint, nameof(options.XrayPoint));
        ArmWaypoint chamber = ArmWaypoint.Parse(options.ChamberPoint, nameof(options.ChamberPoint));
        ArmWaypoint bag = ArmWaypoint.Parse(options.BagPoint, nameof(options.BagPoint));
        ArmWaypoint midScale = ArmWaypoint.Parse(options.MidSafePointScaleAndHome, nameof(options.MidSafePointScaleAndHome));
        ArmWaypoint midXray = ArmWaypoint.Parse(options.MidSafePointXrayAndHome, nameof(options.MidSafePointXrayAndHome));
        ArmWaypoint midChamber = ArmWaypoint.Parse(options.MidSafePointChamberAndHome, nameof(options.MidSafePointChamberAndHome));

        return new Dictionary<ArmMove, IReadOnlyList<ArmWaypoint>>
        {
            // Legacy GSRTPS/GSPX-derived paths: always transition through the mid-safe
            // points so the gripper clears the machine internals.
            [ArmMove.TrayToScale] = [home, midScale, scale, midScale, home],
            [ArmMove.ScaleToAnalyser] = [home, midScale, scale, midScale, home, midXray, xray],
            [ArmMove.AnalyserToChamber] = [xray, midXray, midChamber, chamber, midChamber, home],
            [ArmMove.ChamberToBag] = [midChamber, chamber, midChamber, home, bag, home],
            [ArmMove.ReturnToTray] = [midChamber, chamber, midChamber, home, midScale, scale, midScale, home],
            [ArmMove.Home] = [home],
        };
    }

    private async Task RunAxisForMoveAsync(ArmMove move, CancellationToken cancellationToken)
    {
        if (_akd is null)
        {
            return;
        }

        // The AKD axis shuttles the analyser stage: extend when an item is being presented
        // to the gun, retract (home) when leaving it. Task numbers are drive-stored and
        // provisioning-tuned.
        if (move == ArmMove.ScaleToAnalyser)
        {
            await _akd.MoveTaskAsync(
                    _options.AkdAnalyserMotionTask,
                    _options.AkdMotionTimeoutMs,
                    _options.FeedbackPollIntervalMs,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (move == ArmMove.AnalyserToChamber)
        {
            await _akd.HomeAsync(_options.AkdMotionTimeoutMs, _options.FeedbackPollIntervalMs, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void ReleaseClients()
    {
        _akd?.Dispose();
        _akd = null;
        _motion?.Dispose();
        _motion = null;
        _dashboard?.Dispose();
        _dashboard = null;
        _feedback?.Dispose();
        _feedback = null;
    }
}
