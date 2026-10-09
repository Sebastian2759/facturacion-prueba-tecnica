using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Handlers;

public static class ApiProblems
{
    public static string JsonPath(string path)
        => string.Join('.', path.TrimStart('$', '.').Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    public static ProblemDetails Create(HttpContext context, int status, string title, string code,
        IReadOnlyDictionary<string, string[]>? fields = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "about:blank",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (fields is not null) problem.Extensions["fieldErrors"] = fields;
        return problem;
    }

    public static Task Write(HttpContext context, int status, string title, string code,
        IReadOnlyDictionary<string, string[]>? fields = null)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(Create(context, status, title, code, fields),
            options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
