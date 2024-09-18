using System.Diagnostics;

namespace backend_api.Middlewares
{
    public class RequestTimeLoggingMidleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<UnauthorizedRequestLoggingMiddleware> _logger;

        public RequestTimeLoggingMidleware(RequestDelegate next, ILogger<UnauthorizedRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopWatch = Stopwatch.StartNew();
            await _next.Invoke(context);
            stopWatch.Stop();
            if (stopWatch.ElapsedMilliseconds / 1000 > 4)
            {
                _logger.LogInformation("Request [{Verb}] at {Path} took {Time} ms", context.Request.Method, context.Request.Path, stopWatch.ElapsedMilliseconds);
            }
        }
    }
}
