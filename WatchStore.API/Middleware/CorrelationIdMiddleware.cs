namespace WatchStore.API.Middleware
{
    // יוצר מזהה ייחודי לכל בקשה (או משתמש במזהה שהלקוח שלח),
    // מחזיר אותו ב-header ומכניס אותו ל-scope של הלוג -
    // כך כל שורת לוג שנכתבת במהלך הבקשה, בכל שכבה, כוללת אותו
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId =
                context.Request.Headers.TryGetValue(HeaderName, out var fromClient) &&
                !string.IsNullOrWhiteSpace(fromClient) &&
                fromClient.ToString().Length <= 64
                    ? fromClient.ToString()
                    : Guid.NewGuid().ToString();

            // ה-ExceptionMiddleware שרץ לפני קורא את המזהה מכאן
            context.TraceIdentifier = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                // רק method ו-path - בלי query string, headers או גוף הבקשה,
                // כדי שסיסמאות ו-tokens לא יגיעו ללוג
                _logger.LogInformation(
                    "Incoming request {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await _next(context);

                _logger.LogInformation(
                    "Completed {Method} {Path} with {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode);
            }
        }
    }
}
