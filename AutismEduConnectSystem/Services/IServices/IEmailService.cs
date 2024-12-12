using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Services.IServices
{
    public interface IEmailService
    {
        //public async Task EmailAndLog(string email, string subject, string htmlMessage);
        Task EmailAndLog(EmailLogger emailLogger);
    }
}
