using System.Net;
using System.Text.Json;

namespace WorkspaceStressSystem.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex.StatusCode;

            var payload = new
            {
                error = new
                {
                    code = ex.Code,
                    message = ex.Message,
                    status = ex.StatusCode
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = ex.Message,
                    detail = ex.InnerException?.Message,
                    status = 500
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}