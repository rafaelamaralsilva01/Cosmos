using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shuttle.Bus
{
    public class InMemoryConsumersManager : IDisposable
    {
        private const string BROKER_NAME = "Cosmos";
        private readonly RabbitMQConnection connection;

        private Dictionary<string, List<ConsumerInfo>> consumers;
        private bool disposed;

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

            channel.ExchangeDeclare(
                exchange: BROKER_NAME,
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

        //private void T()
        //{
        //    string rpcResponseQueue = channel.QueueDeclare().QueueName;

        //    string correlationId = Guid.NewGuid().ToString();
        //    string responseFromConsumer = null;

        //    IBasicProperties basicProperties = channel.CreateBasicProperties();
        //    basicProperties.ReplyTo = rpcResponseQueue;
        //    basicProperties.CorrelationId = correlationId;
        //    Console.WriteLine("Enter your message and press Enter.");
        //    string message = Console.ReadLine();
        //    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        //    channel.BasicPublish("", "mycompany.queues.rpc", basicProperties, messageBytes);

        //    EventingBasicConsumer rpcEventingBasicConsumer = new EventingBasicConsumer(channel);
        //    rpcEventingBasicConsumer.Received += (sender, basicDeliveryEventArgs) =>
        //    {
        //        IBasicProperties props = basicDeliveryEventArgs.BasicProperties;
        //        if (props != null
        //            && props.CorrelationId == correlationId)
        //        {
        //            string response = Encoding.UTF8.GetString(basicDeliveryEventArgs.Body);
        //            responseFromConsumer = response;
        //        }
        //        channel.BasicAck(basicDeliveryEventArgs.DeliveryTag, false);
        //        Console.WriteLine("Response: {0}", responseFromConsumer);
        //        Console.WriteLine("Enter your message and press Enter.");
        //        message = Console.ReadLine();
        //        messageBytes = Encoding.UTF8.GetBytes(message);
        //        channel.BasicPublish("", "mycompany.queues.rpc", basicProperties, messageBytes);
        //    };
        //    channel.BasicConsume(rpcResponseQueue, false, rpcEventingBasicConsumer);
        //}

        private ConsumerInfo CreateConsumerInfo(string queueName, Func<string, string, Task> processEvent, Func<string, string, Task> rpcProcessEvent = null)
        {
            var consumer = CreateConsumerChannel(queueName, processEvent);
            return new ConsumerInfo(queueName, consumer);
        }

        private string GetRPCQueueName(string queueName)
        {
            const string RpcDefaultName = "Response";
            return $"{queueName}_{RpcDefaultName}";
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

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (consumers != null)
            {
                foreach (var eventConsumers in consumers)
                {
                    foreach (var consumerInfo in eventConsumers.Value)
                        consumerInfo.Dispose();
                }

                consumers = null;
            }
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

            public IModel RpcConsumer { get; set; }

            public void Dispose()
            {
                Consumer?.Dispose();
                RpcConsumer?.Dispose();
            }
        }
    }
}
