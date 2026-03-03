using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared
{
    public class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var statusCode = StatusCodes.Status500InternalServerError;
            var message = "There is an error occurred from the server.";
            var errors = new List<string>();

            switch (exception)
            {
                case ApiException apiEx:
                    statusCode = apiEx.StatusCode;
                    message = apiEx.Message;
                    break;
                default:
                    if (env.IsDevelopment())
                    {
                        message = exception.Message;
                        errors.Add(exception.StackTrace ?? string.Empty);
                    }
                    break;
            }

            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.FailureResponse(errors, message);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonResponse = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
