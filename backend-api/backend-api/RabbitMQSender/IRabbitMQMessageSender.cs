using backend_api.Models;

namespace backend_api.RabbitMQSender
{
    public interface IRabbitMQMessageSender
    {
        void SendMessage(EmailLogger message, string queueName);
    }
}
