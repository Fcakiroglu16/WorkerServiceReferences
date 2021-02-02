using EmailSenderWorkerService.BackgroundServices;
using EmailSenderWorkerService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSenderWorkerService
{
    public class EmailSenderWorker : BackgroundService
    {
        private readonly ILogger<EmailSenderWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _chanel;

        public EmailSenderWorker(ILogger<EmailSenderWorker> logger, IOptions<RabbitMQOptions> options)
        {
            _logger = logger;
            _rabbitMQOptions = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _connectionFactory = new ConnectionFactory { Uri = new Uri(_rabbitMQOptions.ConnectionString), DispatchConsumersAsync = true };

            _connection = _connectionFactory.CreateConnection();
            _chanel = _connection.CreateModel();

            _chanel.BasicQos(prefetchSize: 0, prefetchCount: 1, false);
            _logger.LogInformation($"Queue listened: {_rabbitMQOptions.QueueName}");
            return base.StartAsync(cancellationToken);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new AsyncEventingBasicConsumer(_chanel);

            consumer.Received += async (model, ea) =>
            {
                var newUserJsonString = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var newUser = JsonSerializer.Deserialize<User>(newUserJsonString);

                    _logger.LogInformation($"Email was send to new  user, user  email:{newUser.Email}");
                    await Task.Delay(1000, stoppingToken);

                    _chanel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException)
                {
                    _logger.LogError($"Json Parsing Error : {newUserJsonString}");
                }
                catch (AlreadyClosedException)
                {
                    _logger.LogInformation("RabbitMQ is closed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email don't send to user");
                }
            };
            _chanel.BasicConsume(_rabbitMQOptions.QueueName, autoAck: false, consumer);
            await Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EmailSenderWorker has stopped");

            _connection.Close();
            return base.StopAsync(cancellationToken);
        }
    }
}