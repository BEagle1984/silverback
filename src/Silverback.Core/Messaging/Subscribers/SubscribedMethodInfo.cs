// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethodInfo
    {
        public SubscribedMethodInfo(MethodInfo methodInfo, bool? exclusive = null, bool? parallel = null, int? maxDegreeOfParallelism = null)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            Parameters = methodInfo.GetParameters();

            if (!Parameters.Any())
                throw new SilverbackException("The subscribed method must have at least 1 argument.");

            var subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

            IsExclusive = subscribeAttribute?.Exclusive ?? exclusive ?? true;
            IsParallel = subscribeAttribute?.Parallel ?? parallel ?? false;
            MaxDegreeOfParallelism = subscribeAttribute?.GetMaxDegreeOfParallelism() ?? maxDegreeOfParallelism;
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