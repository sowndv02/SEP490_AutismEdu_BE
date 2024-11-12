using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Middlewares
{
    public class PaymentCheckerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public PaymentCheckerMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            if (context.Request.Path.StartsWithSegments("/api/v1/PaymentHistory") ||
                context.Request.Path.StartsWithSegments("/api/v1/PackagePayment") ||
                context.Request.Path.StartsWithSegments("/hub/notifications") || (context.Request.Path.StartsWithSegments("/api/v1/user") && 
                context.Request.Method == HttpMethods.Get &&
                context.Request.RouteValues.TryGetValue("id", out var id)))
            {
                await _next(context);
                return;
            }

            // Check if the user is authenticated
            if (context.User.Identity.IsAuthenticated)
            {
                // Check if the endpoint requires authorization
                var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
                var authorizeData = endpoint?.Metadata.GetMetadata<IAuthorizeData>();

                if (authorizeData != null) // Only check if [Authorize] is applied
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var roleClaim = context.User?.FindFirst(ClaimTypes.Role)?.Value;
                        if (roleClaim != null && roleClaim == SD.TUTOR_ROLE)
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                                var paymentHistoryRepository = scope.ServiceProvider.GetRequiredService<IPaymentHistoryRepository>();
                                var resourceService = scope.ServiceProvider.GetRequiredService<IResourceService>();

                                var user = await userRepository.GetAsync(x => x.Id == userId);

                                if (user != null)
                                {
                                    bool isTrialAccount = user.CreatedDate.Date >= DateTime.Now.AddDays(-30).Date;
                                    if (!isTrialAccount)
                                    {
                                        var response = new APIResponse()
                                        {
                                            StatusCode = HttpStatusCode.PaymentRequired,
                                            ErrorMessages = new List<string>() { resourceService.GetString(SD.NEED_PAYMENT_MESSAGE) },
                                            IsSuccess = false
                                        };

                                        context.Response.ContentType = "application/json";
                                        context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                                        await context.Response.WriteAsJsonAsync(response);
                                        await context.Response.CompleteAsync();
                                        return;
                                    }

                                    var (total, list) = await paymentHistoryRepository.GetAllAsync(
                                        x => x.SubmitterId == userId,
                                        "PackagePayment",
                                        pageSize: 1,
                                        pageNumber: 1,
                                        x => x.CreatedDate,
                                        true
                                    );

                                    var latestPaymentHistory = list.FirstOrDefault();
                                    if (latestPaymentHistory != null)
                                    {
                                        bool needPaymentPackage = latestPaymentHistory.ExpirationDate <= DateTime.Now;
                                        if (needPaymentPackage)
                                        {
                                            var response = new APIResponse()
                                            {
                                                StatusCode = HttpStatusCode.PaymentRequired,
                                                ErrorMessages = new List<string>() { resourceService.GetString(SD.NEED_PAYMENT_MESSAGE) },
                                                IsSuccess = false
                                            };

                                            context.Response.ContentType = "application/json";
                                            context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                                            await context.Response.WriteAsJsonAsync(response);
                                            await context.Response.CompleteAsync();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
