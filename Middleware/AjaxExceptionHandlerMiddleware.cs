using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace PawVerse.Middleware
{
    public class AjaxExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AjaxExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public AjaxExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<AjaxExceptionHandlerMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught by AjaxExceptionHandlerMiddleware");
                
                try
                {
                    // Only handle AJAX requests
                    if (IsAjaxRequest(context.Request))
                    {
                        await HandleExceptionAsync(context, ex);
                    }
                    else
                    {
                        // For non-AJAX requests, rethrow the exception to be handled by the default exception handler
                        throw;
                    }
                }
                catch (Exception handlerEx)
                {
                    // Log any errors that occur during exception handling
                    _logger.LogError(handlerEx, "Error in AjaxExceptionHandlerMiddleware while handling an exception");
                    throw; // Let the default exception handler deal with it
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Make sure we haven't already started sending a response
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.",
                    details = _env.IsDevelopment() ? exception.Message : "Lỗi hệ thống",
                    stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };

                try
                {
                    var jsonResponse = JsonSerializer.Serialize(response);
                    await context.Response.WriteAsync(jsonResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error serializing or writing error response");
                    // Fallback to a simple error message if JSON serialization fails
                    await context.Response.WriteAsync("{\"error\":\"Lỗi hệ thống\",\"details\":\"Không thể tạo thông báo lỗi\"}\n");
                }
            }
            else
            {
                _logger.LogWarning("Cannot send error response - headers already sent");
            }
        }

        private bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null)
                return false;
                
            // Check for XMLHttpRequest header
            bool isAjax = request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && 
                          requestedWith.ToString() == "XMLHttpRequest";
                          
            // Check for Accept header containing application/json
            bool acceptsJson = request.Headers.TryGetValue("Accept", out var accept) && 
                              accept.ToString().Contains("application/json");
                              
            return isAjax || acceptsJson;
        }
    }

    // Extension method to make it easier to add the middleware
    public static class AjaxExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseAjaxExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AjaxExceptionHandlerMiddleware>();
        }
    }
}
