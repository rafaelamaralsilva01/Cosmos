using System.Threading.Tasks;
using Earth.Contracts;
using RabbitMQ.Client;
using Shuttle.Bus;

namespace Earth.Aggregates.Food
{
    public class SendFoodSender : IIntegrationEventHandler<FoodMessage>
    {
        public Task Handle(FoodMessage @event)
        {
            return Task.CompletedTask;
        }

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
            var bus = new DiscoveryShuttle(connection);

            // Enviar a mensagem.
            var msg = new FoodMessage("Onions");
            bus.Publish(msg);
        }
    }
}
