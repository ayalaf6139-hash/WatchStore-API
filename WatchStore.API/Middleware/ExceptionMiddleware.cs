using WatchStore.Core.Exceptions;

namespace WatchStore.API.Middleware
{
    // טיפול גלובלי בשגיאות: כל חריגה הופכת לתשובת JSON אחידה עם הסטטוס המתאים.
    // ה-Controllers נשארים רזים ולא צריכים try/catch
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            var correlationId = context.TraceIdentifier;

            var (status, message) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                BusinessRuleException => (StatusCodes.Status400BadRequest, ex.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, ex.Message),
                OperationCanceledException when context.RequestAborted.IsCancellationRequested
                    => (499, "הבקשה בוטלה."),
                _ => (StatusCodes.Status500InternalServerError, "אירעה שגיאה בלתי צפויה")
            };

            // ה-scope של ה-CorrelationId כבר נסגר כשהחריגה הגיעה לכאן, לכן פותחים אותו מחדש
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                if (status == StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(ex, "Unhandled exception");
                }
                else if (status == 499)
                {
                    _logger.LogInformation("Request cancelled by client");
                }
                else
                {
                    // התנגשויות ודחיות עסקיות נרשמות כבר ב-Service ברמת Warning
                    _logger.LogDebug("Handled {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
                }
            }

            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                status,
                message,
                correlationId
            });
        }
    }
}
