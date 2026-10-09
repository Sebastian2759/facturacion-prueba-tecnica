using System.Buffers;

namespace Api.Handlers;

public sealed class RequestBodyLimitMiddleware(RequestDelegate next)
{
    public const int MaximumBytes = 256 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) && !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method))
        {
            await next(context);
            return;
        }
        if (context.Request.ContentLength > MaximumBytes)
        {
            await Reject(context);
            return;
        }
        var original = context.Request.Body;
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        await using var bounded = new MemoryStream();
        try
        {
            int count;
            while ((count = await original.ReadAsync(buffer.AsMemory(), context.RequestAborted)) != 0)
            {
                if (bounded.Length + count > MaximumBytes)
                {
                    await Reject(context);
                    return;
                }
                await bounded.WriteAsync(buffer.AsMemory(0, count), context.RequestAborted);
            }
            bounded.Position = 0;
            context.Request.Body = bounded;
            await next(context);
        }
        finally
        {
            context.Request.Body = original;
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static Task Reject(HttpContext context) => ApiProblems.Write(context, 413,
        "La solicitud supera el máximo de 256 KiB.", "request.too_large");
}
