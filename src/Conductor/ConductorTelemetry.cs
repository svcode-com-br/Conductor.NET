namespace Conductor;

/// <summary>
/// Stable diagnostic names for Conductor.NET activities.
/// </summary>
public static class ConductorTelemetry
{
    /// <summary>
    /// The <c>ActivitySource</c> name used by command dispatch, query dispatch, and notification publishing.
    /// </summary>
    public const string ActivitySourceName = "Conductor";

    /// <summary>
    /// The activity name created for command dispatch.
    /// </summary>
    public const string CommandActivityName = "Conductor.Command";

    /// <summary>
    /// The activity name created for query dispatch.
    /// </summary>
    public const string QueryActivityName = "Conductor.Query";

    /// <summary>
    /// The activity name created for notification publishing.
    /// </summary>
    public const string NotificationActivityName = "Conductor.Notification";

    /// <summary>
    /// Tag name for the request CLR type.
    /// </summary>
    public const string RequestTypeTag = "cqrs.request.type";

    /// <summary>
    /// Tag name for the request kind (<c>command</c>, <c>query</c>, or <c>notification</c>).
    /// </summary>
    public const string RequestKindTag = "cqrs.request.kind";

    /// <summary>
    /// Tag name for the handler CLR type. For notifications this is the handler that failed.
    /// </summary>
    public const string HandlerTypeTag = "cqrs.handler.type";

    /// <summary>
    /// Tag name for the number of handlers resolved for a notification.
    /// </summary>
    public const string HandlerCountTag = "cqrs.handler.count";

    /// <summary>
    /// Tag name for the dispatch outcome.
    /// </summary>
    public const string OutcomeTag = "cqrs.outcome";

    /// <summary>
    /// Tag name for the number of aggregated validation failures.
    /// </summary>
    public const string ValidationFailureCountTag = "cqrs.validation.failure_count";

    /// <summary>
    /// Outcome value for a successful dispatch.
    /// </summary>
    public const string OutcomeSuccess = "success";

    /// <summary>
    /// Outcome value for a technical or handler error.
    /// </summary>
    public const string OutcomeError = "error";

    /// <summary>
    /// Outcome value when dispatch is canceled.
    /// </summary>
    public const string OutcomeCanceled = "canceled";

    /// <summary>
    /// Outcome value when request validation fails.
    /// </summary>
    public const string OutcomeValidationFailed = "validation_failed";
}
