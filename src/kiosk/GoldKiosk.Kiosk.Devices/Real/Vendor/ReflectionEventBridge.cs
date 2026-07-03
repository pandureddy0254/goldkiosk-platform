using System.Linq.Expressions;
using System.Reflection;

namespace GoldKiosk.Kiosk.Devices.Real.Vendor;

/// <summary>
/// Subscribes a callback to an event on a reflection-loaded vendor object without knowing the
/// COM event-handler delegate type at compile time (FlexCode's
/// <c>__FinFPReg_FPRegistrationTemplateEventHandler</c> and friends). The handler's arguments
/// are boxed into an <c>object?[]</c> for the callback.
/// </summary>
internal sealed class ReflectionEventBridge : IDisposable
{
    private readonly object _source;
    private readonly EventInfo _event;
    private readonly Delegate _handler;
    private bool _disposed;

    private ReflectionEventBridge(object source, EventInfo eventInfo, Delegate handler)
    {
        _source = source;
        _event = eventInfo;
        _handler = handler;
    }

    /// <summary>
    /// Attaches <paramref name="callback"/> to the named event, or returns
    /// <see langword="null"/> when the event does not exist on the vendor type.
    /// </summary>
    /// <param name="source">The vendor SDK object.</param>
    /// <param name="eventName">The event name (e.g. <c>FPRegistrationTemplate</c>).</param>
    /// <param name="callback">Receives the event's boxed arguments.</param>
    /// <returns>A subscription that detaches on dispose, or <see langword="null"/>.</returns>
    public static ReflectionEventBridge? TrySubscribe(object source, string eventName, Action<object?[]> callback)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(callback);

        EventInfo? eventInfo = source.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
        if (eventInfo?.EventHandlerType is not { } handlerType)
        {
            return null;
        }

        MethodInfo invoke = handlerType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = [.. invoke.GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"arg{i}"))];
        NewArrayExpression boxedArgs = Expression.NewArrayInit(
            typeof(object),
            parameters.Select(p => (Expression)Expression.Convert(p, typeof(object))));
        Expression body = Expression.Invoke(Expression.Constant(callback), boxedArgs);
        if (invoke.ReturnType != typeof(void))
        {
            body = Expression.Block(body, Expression.Default(invoke.ReturnType));
        }

        Delegate handler = Expression.Lambda(handlerType, body, parameters).Compile();
        eventInfo.AddEventHandler(source, handler);
        return new ReflectionEventBridge(source, eventInfo, handler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _event.RemoveEventHandler(_source, _handler);
        }
        catch (TargetException)
        {
            // The COM object may already be torn down; detaching is best-effort.
        }
    }
}
