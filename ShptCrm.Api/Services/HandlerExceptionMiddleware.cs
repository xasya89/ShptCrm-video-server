using Microsoft.AspNetCore.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ShptCrm.Api.Services
{
    public static class HandlerExceptionMiddleware
    {
        public static void AddHandlerException(this WebApplication app, ILogger logger)
        {
            app.UseExceptionHandler(handleExcpetionApp =>
            {
                handleExcpetionApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerPathFeature>();
                    logger.LogError($"Message  - {exceptionHandlerPathFeature?.Error.Message} \n{exceptionHandlerPathFeature?.Error.StackTrace}");
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        type = "SystemError",
                        message = exceptionHandlerPathFeature?.Error.Message,
                        stackTrace = exceptionHandlerPathFeature?.Error.StackTrace
                    }));
                });
            });
        }
    }
}
