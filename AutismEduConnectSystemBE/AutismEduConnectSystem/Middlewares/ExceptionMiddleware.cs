using AutismEduConnectSystem.Exceptions;
using AutismEduConnectSystem.Models;
using Newtonsoft.Json;
using System.Net;

namespace AutismEduConnectSystem.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            var response = new APIResponse()
            {
                ErrorMessages = new List<string> { ex.Message },
                IsSuccess = false
            };
            switch (ex)
            {
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add(notFoundException.Message);
                    break;
                default:
                    break;
            }

            string responseJson = JsonConvert.SerializeObject(response);
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseJson);
        }
    }
}
