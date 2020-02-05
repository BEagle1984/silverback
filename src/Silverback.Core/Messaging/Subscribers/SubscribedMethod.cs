// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethod
    {
        private readonly Func<IServiceProvider, object> _targetTypeFactory;

        public SubscribedMethod(
            Func<IServiceProvider, object> targetTypeFactory,
            MethodInfo methodInfo,
            bool? exclusive,
            bool? parallel,
            int? maxDegreeOfParallelism)
        {
            _targetTypeFactory = targetTypeFactory ?? throw new ArgumentNullException(nameof(targetTypeFactory));
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            Parameters = methodInfo.GetParameters();
      
            if (!Parameters.Any())
                throw new SilverbackException("The subscribed method must have at least 1 argument.");

            Filters = methodInfo.GetCustomAttributes<MessageFilterAttribute>(false).ToList();
            
            IsExclusive = exclusive ?? true;
            IsParallel = parallel ?? false;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] Parameters { get; }

        /// <summary>See <see cref="SubscribeAttribute" />.</summary>
        public bool IsExclusive { get; }

        /// <summary>See <see cref="SubscribeAttribute" />.</summary>
        public bool IsParallel { get; }

        /// <summary>See <see cref="SubscribeAttribute" />.</summary>
        public int? MaxDegreeOfParallelism { get; }
        
        public IReadOnlyCollection<IMessageFilter> Filters { get; } 

        public object ResolveTargetType(IServiceProvider serviceProvider) =>
            _targetTypeFactory(serviceProvider);
    }
}