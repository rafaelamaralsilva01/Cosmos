using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Shuttle.Bus
{
    public class DiscoveryShuttle : IShuttle
    {
        private const string BROKER_NAME = "Cosmos";
        private readonly RabbitMQConnection connection;

        public DiscoveryShuttle(RabbitMQConnection connection)
        {
            this.connection = connection;
        }

        public void Publish(IntegrationEvent message)
        {
            if (!connection.IsConnected)
            {
                connection.TryConnect();
            }

            var policy = RetryPolicy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(
                    5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        // _logger.LogWarning(ex.ToString());
                    });

            using (var channel = connection.CreateModel())
            {
                var eventName = message.GetType().Name;

                channel.ExchangeDeclare(
                    BROKER_NAME,
                    type: "direct");

                var @event = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(@event);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    channel.BasicPublish(exchange: BROKER_NAME,
                                     routingKey: eventName,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }

        public void Subscribe<T, TH>() 
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
        }
    }
}
