using System;

namespace Shuttle.Bus
{
    public interface IShuttle : IBus
    {
        string Publish(IntegrationEvent message, Func<string> process);
    }
}
