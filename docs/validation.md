# Validation

Conductor.NET owns a small validator abstraction so the core package does not require FluentValidation.

## Semantics

- All validators registered for the request run sequentially.
- Failures are aggregated into `RequestValidationException`.
- The handler is not invoked when any failure is present.
- Validators should not modify state.
- Authorization is not validation.
- Missing records are normally a handler or domain-result concern.

## ASP.NET Core

`Conductor.AspNetCore` maps `RequestValidationException` to HTTP 400 ProblemDetails:

```csharp
builder.Services.AddConductorProblemDetails();
app.UseExceptionHandler();
```

Missing handlers and duplicate-handler configuration errors are not converted to 4xx responses. Cancellation is not treated as validation.

## FluentValidation

An adapter package is not included in this release. Applications may implement `IValidator<TRequest>` by wrapping another library.
