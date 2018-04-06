namespace Silverback
{
    /// <summary>
    /// Exposes the interface to let verify the object's configuration.
    /// </summary>
    public interface IConfigurationValidation
    {
        /// <summary>
        /// Called after the fluent configuration is applied, should verify the consistency of the 
        /// configuration.
        /// </summary>
        /// <remarks>An exception must be thrown if the confgiuration is not conistent.</remarks>
        void ValidateConfiguration();
    }
}