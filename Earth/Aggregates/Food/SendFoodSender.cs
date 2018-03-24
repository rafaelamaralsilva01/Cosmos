﻿using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Earth.Aggregates.Food
{
    public class SendFoodSender
    {
        private IBus bus;

        public void Send()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            var connection = new RabbitMQConnection(factory);
            connection.TryConnect();
            var bus = new Bus(connection);

            // Enviar a mensagem.
            var msg = new SendFoodMessage("Onions");
            bus.Publish(msg);
        }
    }

    public class Bus : IBus
    {
        private const string BROKER_NAME = "Cosmos";
        private readonly RabbitMQConnection connection;

        public Bus(RabbitMQConnection connection)
        {
            this.connection = connection;
        }

        public void Publish<T>(T message)
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
    }

    public class RabbitMQConnection : IDisposable
    {
        private readonly IConnectionFactory connectionFactory;

        private readonly int retryCount = 5;
        IConnection connection;
        bool disposed;

        object sync_root = new object();

        public RabbitMQConnection(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public bool IsConnected
        {
            get
            {
                return connection != null && connection.IsOpen && !disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return connection.CreateModel();
        }

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            try
            {
                connection.Dispose();
            }
            catch (IOException ex)
            {
                // _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        // _logger.LogWarning(ex.ToString());
                    }
                );

                policy.Execute(() =>
                {
                    connection = connectionFactory
                          .CreateConnection();
                });

                if (IsConnected)
                {
                    connection.ConnectionShutdown += OnConnectionShutdown;
                    connection.CallbackException += OnCallbackException;
                    connection.ConnectionBlocked += OnConnectionBlocked;

                    // _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");

                    return true;
                }
                else
                {
                    // _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (disposed) return;

            // _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (disposed) return;

            // _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (disposed) return;

            // _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
