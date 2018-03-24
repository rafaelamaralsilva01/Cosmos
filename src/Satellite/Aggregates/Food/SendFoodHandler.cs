using Earth.Contracts;
using Shuttle.Bus;
using System.Threading.Tasks;

namespace Satellite.Aggregates.Food
{
    public class SendFoodHandler : IIntegrationEventHandler<FoodMessage>
    {
        public Task Handle(FoodMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
