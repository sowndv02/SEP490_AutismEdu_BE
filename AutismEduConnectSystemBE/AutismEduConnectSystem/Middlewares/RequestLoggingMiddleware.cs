﻿namespace AutismEduConnectSystem.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Check if the request method is POST, PUT, or DELETE
            if (context.Request.Method == HttpMethods.Post ||
                context.Request.Method == HttpMethods.Put ||
                context.Request.Method == HttpMethods.Delete)
            {
                // Log request details (URL, headers, etc.)

                // Extract userID from token if available
                var userId = context.User?.FindFirst("userID")?.Value; // Replace "userID" with the actual claim type name
                if (!string.IsNullOrEmpty(userId))
                {
                }
                else
                {
                }

                // Optionally, log request body if needed
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0; // Reset stream position for next middleware
            }

            await _next(context);
        }

    }
}
