using Earth.Contracts;
using Shuttle.Bus;
using System.Threading.Tasks;

namespace Satellite.Aggregates.Technologies
{
    public class AskForTechHandler : IIntegrationEventHandler<AskForTechMessage>
    {
        public Task Handle(AskForTechMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
