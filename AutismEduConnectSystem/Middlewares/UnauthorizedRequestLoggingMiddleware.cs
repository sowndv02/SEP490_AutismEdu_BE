namespace AutismEduConnectSystem.Middlewares
{
    public class UnauthorizedRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UnauthorizedRequestLoggingMiddleware> _logger;

        public UnauthorizedRequestLoggingMiddleware(RequestDelegate next, ILogger<UnauthorizedRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Log incoming request
            var requestHeaders = context.Request.Headers
                .Select(header => $"{header.Key}: {header.Value}")
                .ToArray();
            _logger.LogInformation("Request: {Method} {Path} by {User} with headers: {Headers}",
                context.Request.Method,
                context.Request.Path,
                context.User.Identity?.Name ?? "Anonymous",
                string.Join(", ", requestHeaders));

            // Proceed with the request pipeline
            await _next(context);

            // Log response status and other relevant info after the request is processed
            var responseHeaders = context.Response.Headers
                .Select(header => $"{header.Key}: {header.Value}")
                .ToArray();
            _logger.LogInformation("Response: {StatusCode} for {Method} {Path} by {User} with headers: {Headers}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                context.User.Identity?.Name ?? "Anonymous",
                string.Join(", ", responseHeaders));
        }
    }

}
