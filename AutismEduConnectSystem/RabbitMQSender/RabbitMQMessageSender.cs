using AutismEduConnectSystem.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace AutismEduConnectSystem.RabbitMQSender
{
    public class RabbitMQMessageSender: IRabbitMQMessageSender
    {
        private readonly string _hostName;
        private readonly string _userName;
        private readonly string _password;
        private IConnection _connection;
        public RabbitMQMessageSender(IConfiguration configuration)
        {
            var rabbitMQSettings = configuration.GetSection("RabbitMQSettings");
            _hostName = rabbitMQSettings["HostName"];
            _userName = rabbitMQSettings["UserName"];
            _password = rabbitMQSettings["Password"];
        }
        public void SendMessage(EmailLogger message, string queueName)
        {
            if (ConnectionExists())
            {
                using var channel = _connection.CreateModel();
                channel.QueueDeclare(queueName, false, false, false, null);
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                Console.WriteLine($"Publishing email message to RabbitMQ: {body}");
                channel.BasicPublish(exchange: "", routingKey: queueName, null, body: body);
            }
        }

        private void CreateConnection()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    UserName = _userName,
                    Password = _password
                };
                _connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private bool ConnectionExists()
        {
            if (_connection != null)
            {
                return true;
            }
            CreateConnection();
            return true;
        }
    }
}
