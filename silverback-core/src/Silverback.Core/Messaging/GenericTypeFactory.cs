using System;
using System.Linq;

namespace Silverback.Messaging
{
    /// <summary>
    /// Wraps a <see cref="Func{T, TResult}"/> that is used to dinamically instanciate the types needed to handle the messages.
    /// </summary>
    /// <seealso cref="ITypeFactory" />
    public class GenericTypeFactory : ITypeFactory
    {
        private readonly Func<Type, object> _singleInstanceFactory;
        private readonly Func<Type, object[]> _multiInstancesFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTypeFactory" /> class.
        /// </summary>
        /// <param name="singleInstanceFactory">The actual factory method used to retrieve a single instance.</param>
        /// <param name="multiInstancesFactory">The actual factory method used to retrieve all instances of a type.</param>
        public GenericTypeFactory(Func<Type, object> singleInstanceFactory, Func<Type, object[]> multiInstancesFactory)
        {
            _singleInstanceFactory = singleInstanceFactory ?? throw new ArgumentNullException(nameof(singleInstanceFactory));
            _multiInstancesFactory = multiInstancesFactory ?? throw new ArgumentNullException(nameof(multiInstancesFactory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTypeFactory" /> class.
        /// </summary>
        /// <param name="singleInstanceFactory">The actual factory method used to retrieve a single instance.</param>
        /// <exception cref="ArgumentNullException">
        /// singleInstanceFactory
        /// or
        /// multiInstancesFactory
        /// </exception>
        public GenericTypeFactory(Func<Type, object> singleInstanceFactory)
        {
            _singleInstanceFactory = singleInstanceFactory ?? throw new ArgumentNullException(nameof(singleInstanceFactory));
            _multiInstancesFactory = t => new[] { singleInstanceFactory(t) };
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTypeFactory" /> class.
        /// </summary>
        /// <param name="multiInstancesFactory">The actual factory method used to retrieve all instances of a type.</param>
        /// <exception cref="ArgumentNullException">
        /// singleInstanceFactory
        /// or
        /// multiInstancesFactory
        /// </exception>
        public GenericTypeFactory(Func<Type, object[]> multiInstancesFactory)
        {
            _multiInstancesFactory = multiInstancesFactory ?? throw new ArgumentNullException(nameof(multiInstancesFactory));
            _singleInstanceFactory = t => multiInstancesFactory.Invoke(t).FirstOrDefault();
        }

        /// <summary>
        /// Returns an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to be instantiated.</param>
        /// <returns></returns>
        public object GetInstance(Type type)
            => _singleInstanceFactory(type);

        /// <summary>
        /// Returns an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to be instantiated.</typeparam>
        /// <returns></returns>
        public T GetInstance<T>()
            => (T)_singleInstanceFactory(typeof(T));

        /// <summary>
        /// Returns all instances of the specified type.
        /// </summary>
        /// <param name="type">The type to be instantiated.</param>
        /// <returns></returns>
        public object[] GetInstances(Type type)
            => _multiInstancesFactory(type);

        /// <summary>
        /// Gets the instances.
        /// </summary>
        /// <typeparam name="T">The type to be instantiated.</typeparam>
        /// <returns></returns>
        public T[] GetInstances<T>()
            => _multiInstancesFactory(typeof(T)).Cast<T>().ToArray();
    }
}