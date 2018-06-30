namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Implement this inteface to outsource some <see cref="IBus"/> configuration into a separate file.
    /// </summary>
    public interface IConfigurator
    {
        /// <summary>
        /// Configures the <see cref="IBus"/>.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/> to be configured.</param>
        void Configure(IBus bus);
    }
}