﻿using System.Diagnostics;

namespace AutismEduConnectSystem.Middlewares
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
            }
        }
    }
}
