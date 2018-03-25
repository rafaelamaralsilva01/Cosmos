using Autofac;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shuttle.Bus
{
    public class DiscoveryShuttle : IShuttle
    {
        private const string BROKER_NAME = "Cosmos";
        private readonly RabbitMQConnection connection;
        private readonly InMemoryEventBusSubscriptionsManager subsManager;
        private readonly InMemoryConsumersManager consumerManager;
        private readonly ILifetimeScope autofac;

        public DiscoveryShuttle(RabbitMQConnection connection, string queueName, ILifetimeScope autofac)
        {
            this.connection = connection;
            this.autofac = autofac;
            this.subsManager = new InMemoryEventBusSubscriptionsManager();
            this.consumerManager = new InMemoryConsumersManager(connection);
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
                    type: "topic");

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
            var eventName = subsManager.GetEventKey<T>();
            var queueName = consumerManager.GetQueueName<T, TH>();

            consumerManager.Add<T, TH>(ProcessEvent);
            DoInternalSubscription(eventName, queueName);
            subsManager.AddSubscription<T, TH>();
        }

        private void DoInternalSubscription(string eventName, string queueName)
        {
            var containsKey = subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!connection.IsConnected)
                {
                    connection.TryConnect();
                }

                using (var channel = connection.CreateModel())
                {
                    channel.QueueBind(queue: queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (subsManager.HasSubscriptionsForEvent(eventName))
            {
                using (var scope = autofac.BeginLifetimeScope())
                {
                    var subscriptions = subsManager.GetHandlersForEvent(eventName);
                    foreach (var subscription in subscriptions)
                    {
                        var eventType = subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        if (handler != null)
                        {
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        }
                    }
                }
            }
        }
    }
}
