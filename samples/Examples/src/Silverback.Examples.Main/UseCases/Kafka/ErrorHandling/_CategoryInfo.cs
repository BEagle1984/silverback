﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;
using Silverback.Examples.Main.UseCases.ErrorHandling;

namespace Silverback.Examples.Main.UseCases.Kafka.ErrorHandling
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class _CategoryInfo : ICategory
    {
        public string Title => "Error policies";
        public string Description => "The error policies are quite a powerful tool to make your consumer resilient " +
                                     "and fault tolerant. They provide an easy way to instruct Silverback how to " +
                                     "behave in case an exception is thrown by the method processing the incoming " +
                                     "message.";
        
        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(RetryAndMoveErrorPolicyUseCase),
            typeof(RetryAndMoveErrorPolicyUseCase2),
        };
    }
}
