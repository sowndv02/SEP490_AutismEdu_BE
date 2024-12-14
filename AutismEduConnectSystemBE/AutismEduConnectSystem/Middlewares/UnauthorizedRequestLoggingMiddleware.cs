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
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {

                var headers = context.Request.Headers
                    .Select(header => $"{header.Key}: {header.Value}")
                    .ToArray();

            }
        }
    }

}
