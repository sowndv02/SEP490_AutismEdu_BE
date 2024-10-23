using backend_api.Models;

namespace backend_api.Services.IServices
{
    public interface IEmailService
    {
        //public async Task EmailAndLog(string email, string subject, string htmlMessage);
        Task EmailAndLog(EmailLogger emailLogger);
    }
}
