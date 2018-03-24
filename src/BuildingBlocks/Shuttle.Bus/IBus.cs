using System;

namespace Shuttle.Bus
{
    public interface IBus
    {
        void Publish<T>(T message) where T : IMessage;
        void Subscribe<T>(T message, Action<T> handler) where T : IMessage;
    }
}
