using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkerServiceApp.Web.Models;

namespace WorkerServiceApp.Web.BackgroundServices
{
    /// <summary>
    /// Background olarak herhangi bir iş yapacak örnek bir background sınıfı
    /// </summary>
    public class SampleBackgroundService : BackgroundService
    {
        private readonly ILogger<SampleBackgroundService> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _chanel;

        public SampleBackgroundService(ILogger<SampleBackgroundService> logger, IOptions<RabbitMQOptions> options)
        {
            _logger = logger;
            _rabbitMQOptions = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SampleBackgroundService has started");

            _connectionFactory = new ConnectionFactory { Uri = new Uri(_rabbitMQOptions.ConnectionString) };

            _connection = _connectionFactory.CreateConnection();
            _chanel = _connection.CreateModel();

            _chanel.QueueDeclare(_rabbitMQOptions.QueueName, durable: true, exclusive: false, autoDelete: false);

            return base.StartAsync(cancellationToken);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SampleBackgroundService  ExecuteAsync has worked");

            var properties = _chanel.CreateBasicProperties();

            properties.Persistent = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                var randomNumber = new Random().Next(1, 1000);
                User newUser = new() { Id = randomNumber, UserName = $"User-{randomNumber}", Email = $"{randomNumber}@hotmail.com" };

                var jsonUser = JsonSerializer.Serialize<User>(newUser);

                var byteUser = Encoding.UTF8.GetBytes(jsonUser);

                _chanel.BasicPublish("", _rabbitMQOptions.QueueName, properties, byteUser);

                _logger.LogInformation($"UserCreatedEvent has raised:  user Id:  {newUser.Id}");
                await Task.Delay(1000);
            }

            #region WriteToFileExample

            //Write  to File Example
            //var count = 0;
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    count++;

            //    await WriteToFileAsync(count);
            //}

            #endregion WriteToFileExample
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SampleBackgroundService has stopped");

            _connection.Close();
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Write To File method example
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private async Task WriteToFileAsync(int count)
        {
            await File.AppendAllTextAsync("log.txt", $"log {count}\n");
            await Task.Delay(1000);
        }
    }
}