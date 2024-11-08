using backend_api.Models;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Security.Claims;

namespace backend_api.Middlewares
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
            if (context.User.Identity.IsAuthenticated)
            {
                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var paymentHistoryRepository = scope.ServiceProvider.GetRequiredService<IPaymentHistoryRepository>();
                        var resourceService = scope.ServiceProvider.GetRequiredService<IResourceService>();

                        var user = await userRepository.GetAsync(x => x.Id == userId);

                        if (user != null)
                        {
                            bool isTrialAccount = user.CreatedDate <= DateTime.Now.AddDays(-30);
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
                                if (needPaymentPackage && !isTrialAccount)
                                {
                                    var response = new APIResponse()
                                    {
                                        StatusCode = HttpStatusCode.PaymentRequired,
                                        ErrorMessages = new List<string>() { resourceService.GetString(SD.NEED_PAYMENT_MESSAGE) },
                                        IsSuccess = false
                                    };

                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsJsonAsync(response);
                                    return;
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