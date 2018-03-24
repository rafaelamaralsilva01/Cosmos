using System.Threading.Tasks;
using Earth.Contracts;
using RabbitMQ.Client;
using Shuttle.Bus;

namespace Earth.Aggregates.Food
{
    public class SendFoodSender
    {
        private readonly DiscoveryShuttle shuttle;

        public SendFoodSender(DiscoveryShuttle shuttle)
        {
            this.shuttle = shuttle;
        }

        public void Send()
        {            
            // Enviar a mensagem.
            var msg = new FoodMessage("Onions");
            shuttle.Publish(msg);
        }
    }
}
