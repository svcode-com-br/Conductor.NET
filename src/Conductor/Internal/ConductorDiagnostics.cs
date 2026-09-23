using System.Diagnostics;

namespace Conductor.Internal;

internal static class ConductorDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new(ConductorTelemetry.ActivitySourceName);

    internal static Activity? StartCommand(Type requestType) =>
        Start(ConductorTelemetry.CommandActivityName, "command", requestType);

    internal static Activity? StartQuery(Type requestType) =>
        Start(ConductorTelemetry.QueryActivityName, "query", requestType);

    internal static Activity? StartNotification(Type notificationType) =>
        Start(ConductorTelemetry.NotificationActivityName, "notification", notificationType);

    internal static void SetHandlerType(Activity? activity, Type handlerType)
    {
        activity?.SetTag(ConductorTelemetry.HandlerTypeTag, handlerType.FullName);
    }

    internal static void SetOutcome(Activity? activity, string outcome)
    {
        activity?.SetTag(ConductorTelemetry.OutcomeTag, outcome);
    }

    internal static void SetValidationFailureCount(Activity? activity, int count)
    {
        activity?.SetTag(ConductorTelemetry.ValidationFailureCountTag, count);
    }

    internal static void SetHandlerCount(Activity? activity, int count)
    {
        activity?.SetTag(ConductorTelemetry.HandlerCountTag, count);
    }

    private static Activity? Start(string name, string kind, Type requestType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag(ConductorTelemetry.RequestTypeTag, requestType.FullName);
        activity.SetTag(ConductorTelemetry.RequestKindTag, kind);
        return activity;
    }
}
