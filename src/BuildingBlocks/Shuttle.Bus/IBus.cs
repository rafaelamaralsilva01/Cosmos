namespace Shuttle.Bus
{
    public interface IBus
    {
        void Publish<T>(T message);
    }
}
