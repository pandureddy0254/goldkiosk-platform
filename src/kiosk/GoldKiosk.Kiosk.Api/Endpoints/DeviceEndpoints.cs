using GoldKiosk.Contracts.V1.Devices;
using GoldKiosk.Kiosk.Api.Devices;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Devices.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Device health endpoints (payload samples §12): the fleet snapshot including per-device
/// mode (real/mock — ADR 0004), and the single-device diagnostic probe.
/// </summary>
public static class DeviceEndpoints
{
    /// <summary>Maps the device endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapDeviceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/devices", GetSnapshot);

        group.MapPost("/devices/{key}/probe", ProbeAsync)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static Ok<DevicesSnapshotResponse> GetSnapshot(IDeviceRegistry registry)
    {
        var devices = registry.All.Select(DeviceStatusMapper.ToDto).ToList();
        return TypedResults.Ok(
            new DevicesSnapshotResponse(DeviceStatusMapper.Overall(registry.All), devices));
    }

    private static async Task<Results<Ok<DeviceProbeResponse>, ProblemHttpResult>> ProbeAsync(
        string key,
        IDeviceRegistry registry,
        CancellationToken cancellationToken)
    {
        IKioskDevice device;
        try
        {
            device = registry.Get(key);
        }
        catch (KeyNotFoundException)
        {
            return Problems.DeviceNotFound(key);
        }

        DeviceProbeResult result = await device.ProbeAsync(cancellationToken);
        return TypedResults.Ok(new DeviceProbeResponse(
            device.Key, result.Passed ? "pass" : "fail", result.ElapsedMs));
    }
}
