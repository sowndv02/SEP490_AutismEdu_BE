using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.RabbitMQSender
{
    public interface IEmailSender
    {
        void SendMessage(EmailLogger message, string queueName);
    }
}
