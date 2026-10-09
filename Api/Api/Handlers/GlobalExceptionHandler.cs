using Domain.Base;
using Domain.Contracts.Accounts;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Handlers;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                var fields = validation.Errors.GroupBy(error => ApiProblems.JsonPath(error.PropertyName))
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
                await ApiProblems.Write(httpContext, 400, "Revise los campos indicados.", "validation.failed", fields);
                break;
            case AccessDeniedException:
                await ApiProblems.Write(httpContext, 403, "No tiene permiso para esta operación.", "access.denied");
                break;
            case DomainException domain:
                await ApiProblems.Write(httpContext, 422, "Los datos no cumplen las reglas de la operación.", "domain.invalid",
                    domain.Errors.ToDictionary(pair => ApiProblems.JsonPath(pair.Key), pair => new[] { pair.Value }));
                break;
            case AccountsIntegrationException provider:
                await ApiProblems.Write(httpContext, provider.HttpStatus, provider.Message, provider.Code);
                break;
            case BadHttpRequestException request:
                await ApiProblems.Write(httpContext, request.StatusCode, "La solicitud no es válida.", "request.invalid");
                break;
            case OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested:
                httpContext.Response.StatusCode = 499;
                break;
            default:
                // Exception objects can contain connection strings or customer data.
                LogUnexpected(logger, exception.GetType().Name, httpContext.TraceIdentifier);
                await ApiProblems.Write(httpContext, 500, "No se pudo completar la operación. Use el código de seguimiento.", "server.error");
                break;
        }
        return true;
    }
    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unexpected failure {ExceptionType}, trace {TraceId}")]
    private static partial void LogUnexpected(ILogger logger, string exceptionType, string traceId);
}
