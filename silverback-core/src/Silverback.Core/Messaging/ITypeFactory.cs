using System;

namespace Silverback.Messaging
{
    /// <summary>
    /// Provides an instance of the specified type and is used to resolve message handlers and other types that
    /// need to be dinamically instanciated.
    /// </summary>
    public interface ITypeFactory
    {
        /// <summary>
        /// Returns an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to be instanciated.</param>
        /// <returns></returns>
        object GetInstance(Type type);
    }
}
