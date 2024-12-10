using AutismEduConnectSystem.Services.IServices;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using AutismEduConnectSystem.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace AutismEduConnectSystem.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private IConnection _connection;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IModel _channel;
        public RabbitMQConsumer(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_configuration.GetValue<string>("RabbitMQSettings:QueueName"), false, false, false, null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                EmailLogger emailLogger = JsonConvert.DeserializeObject<EmailLogger>(content);
                HandleMessage(emailLogger);

                _channel.BasicAck(ea.DeliveryTag, false);
            };
            _channel.BasicConsume(_configuration.GetValue<string>("RabbitMQSettings:QueueName"), false, consumer);
            return Task.CompletedTask;
        }

        private void HandleMessage(EmailLogger emailLogger)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                emailService.EmailAndLog(emailLogger).GetAwaiter().GetResult();
            }
        }
    }
}
