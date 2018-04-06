using System;

namespace Silverback.Messaging
{
    /// <summary>
    /// Wraps a <see cref="Func{T, TResult}"/> that is used to dinamically instanciate the types needed to handle the messages.
    /// </summary>
    /// <seealso cref="ITypeFactory" />
    public class GenericTypeFactory : ITypeFactory
    {
        private readonly Func<Type, object> _actualProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTypeFactory" /> class.
        /// </summary>
        /// <param name="actualProvider">The actual provider method.</param>
        public GenericTypeFactory(Func<Type, object> actualProvider)
        {
            _actualProvider = actualProvider ?? throw new ArgumentNullException(nameof(actualProvider));
        }

        /// <summary>
        /// Returns an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to be instanciated.</param>
        /// <returns></returns>
        public object GetInstance(Type type)
            => _actualProvider(type);
    }
}