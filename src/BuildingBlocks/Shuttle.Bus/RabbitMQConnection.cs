using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace Shuttle.Bus
{
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
                    connection = connectionFactory.CreateConnection();
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
