using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shuttle.Bus
{
    public class InMemoryConsumersManager
    {
        private const string BROKER_NAME = "Cosmos";
        private readonly Dictionary<string, List<ConsumerInfo>> consumers;
        private readonly RabbitMQConnection connection;

        public InMemoryConsumersManager(RabbitMQConnection connection)
        {
            this.connection = connection;
            this.consumers = new Dictionary<string, List<ConsumerInfo>>();
        }

        public void Add<T, TH>(Func<string, string, Task> processEvent)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetKey<T>();
            string queueName = GetQueueName<T, TH>();
            if (!HasConsumersForEvent(eventName))
            {
                consumers.Add(eventName, new List<ConsumerInfo>());
            }

            var consumersList = consumers[eventName];
            if (!consumersList.Any(info => info.QueueName == queueName))
            {
                var info = new ConsumerInfo(queueName, CreateConsumerChannel(queueName, processEvent));
                consumersList.Add(info);
            }
        }

        private IModel CreateConsumerChannel(string queueName, Func<string, string, Task> processEvent)
        {
            if (!connection.IsConnected)
            {
                connection.TryConnect();
            }

            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                 type: "topic");

            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);

                await processEvent(eventName, message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);

            channel.CallbackException += (sender, ea) =>
            {
                channel.Dispose();
                channel = CreateConsumerChannel(queueName, processEvent);
            };

            return channel;
        }


        public bool HasConsumersForEvent(string eventName) => consumers.ContainsKey(eventName);

        public string GetKey<T>()
        {
            return typeof(T).Name;
        }

        public string GetQueueName<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            return $"{typeof(TH).Name}_{typeof(T).Name}";
        }

        class ConsumerInfo : IDisposable
        {
            public ConsumerInfo(string queueName, IModel consumer)
            {
                this.QueueName = queueName;
                this.Consumer = consumer;
            }

            public string QueueName { get; set; }

            public IModel Consumer { get; set; }

            public void Dispose()
            {
                Consumer?.Dispose();
            }
        }
    }

    
}
