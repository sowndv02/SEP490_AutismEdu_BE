using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.RabbitMQSender
{
    public interface IRabbitMQMessageSender
    {
        void SendMessage(EmailLogger message, string queueName);
    }
}
