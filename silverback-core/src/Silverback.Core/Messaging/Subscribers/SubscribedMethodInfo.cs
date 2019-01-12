// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethodInfo
    {
        // TODO: Overload to pass exclusive and parallel for non attribute based registrations
        public SubscribedMethodInfo(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            Parameters = methodInfo.GetParameters();

            if (!Parameters.Any())
                throw new SilverbackException("The subscribed method must have at least 1 argument.");

            var subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

            IsExclusive = subscribeAttribute?.Exclusive ?? true;
            IsParallel = subscribeAttribute?.Parallel ?? false;
            MaxDegreeOfParallelism = subscribeAttribute?.MaxDegreeOfParallelism;
        }

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] Parameters { get; }

        /// <summary>See <see cref="SubscribeAttribute"/>.</summary>
        public bool IsExclusive { get; }

        /// <summary>See <see cref="SubscribeAttribute"/>.</summary>
        public bool IsParallel { get; }

        /// <summary>See <see cref="SubscribeAttribute"/>.</summary>
        public int? MaxDegreeOfParallelism { get; }
    }
}