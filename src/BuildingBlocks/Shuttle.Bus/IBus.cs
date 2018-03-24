using System;

namespace Shuttle.Bus
{
    public interface IBus
    {
        void Publish(IntegrationEvent message);
        void Subscribe<T, TH>() 
            where T : IntegrationEvent 
            where TH : IIntegrationEventHandler<T>;
    }
}
