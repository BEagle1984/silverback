namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Implement this inteface to outsource some <see cref="IBus"/> configuration into a separate file.
    /// </summary>
    public interface IConfigurator
    {
        void Configure(IBus bus);
    }
}