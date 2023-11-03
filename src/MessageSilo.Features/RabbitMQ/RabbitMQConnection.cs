﻿using Azure.Messaging.ServiceBus;
using MessageSilo.Shared.Enums;
using MessageSilo.Shared.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSilo.Features.RabbitMQ
{
    public class RabbitMQConnection : MessagePlatformConnection
    {
        private readonly ILogger logger;

        private IConnection connection;

        private IModel channel;

        public string QueueName { get; }

        public string ExchangeName { get; }

        public RabbitMQConnection(string connectionString, string queueName, string exchangeName, ReceiveMode rm, ILogger logger)
        {
            ConnectionString = connectionString;
            QueueName = queueName;
            ExchangeName = exchangeName;
            ReceiveMode = rm;
            this.logger = logger;
        }

        public override async ValueTask DisposeAsync()
        {
            if (channel is not null)
                channel.Dispose();

            if (connection is not null)
                connection.Dispose();

            await Task.CompletedTask;
        }

        public override async Task Enqueue(Message message)
        {
            channel.BasicPublish(exchange: ExchangeName ?? string.Empty,
                     routingKey: ExchangeName is null ? QueueName : string.Empty,
                     basicProperties: channel.CreateBasicProperties(),
                     body: Encoding.UTF8.GetBytes(message.Body));

            await Task.CompletedTask;
        }

        public override async Task Init()
        {
            var factory = new ConnectionFactory { Uri = new Uri(ConnectionString) };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            if (ReceiveMode != ReceiveMode.None)
            {
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    string body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();

                    OnMessageReceived(new MessageReceivedEventArgs(new Message(messageId, body)));
                };

                channel.BasicConsume(queue: QueueName,
                                     autoAck: autoAck,
                                     consumer: consumer);

            }

            await Task.CompletedTask;
        }
    }
}
