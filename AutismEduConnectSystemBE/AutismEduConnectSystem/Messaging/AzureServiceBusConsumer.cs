using Azure.Messaging.ServiceBus;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Services;
using Newtonsoft.Json;
using System.Text;

namespace AutismEduConnectSystem.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string emailQueue;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private ServiceBusProcessor _processorEmail;

        public AzureServiceBusConsumer(IConfiguration configuration, EmailService emailService)
        {
            _emailService = emailService;
            _configuration = configuration;
            serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
            emailQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailQueue");

            var client = new ServiceBusClient(serviceBusConnectionString);
            _processorEmail = client.CreateProcessor(emailQueue);
        }

        public async Task Start()
        {
            _processorEmail.ProcessMessageAsync += OnEmailRequestReceivied;
            _processorEmail.ProcessErrorAsync += ErrorHandler;
            await _processorEmail.StartProcessingAsync();
        }



        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            await _processorEmail.StopProcessingAsync();
            await _processorEmail.DisposeAsync();
        }

        private async Task OnEmailRequestReceivied(ProcessMessageEventArgs args)
        {
            // This is where you will receive message
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            EmailLogger objMessage = JsonConvert.DeserializeObject<EmailLogger>(body);
            try
            {
                // Try to log email
                await _emailService.EmailAndLog(objMessage);
                await args.CompleteMessageAsync(args.Message);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }

}
