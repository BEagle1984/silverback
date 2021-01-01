// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    // TODO: Convert to short record declaration as soon as the StyleCop.Analyzers is updated,
    //       see https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3181
    // internal record LogAdditionalArgument<TEnvelope>(
    //     string Name,
    //     Func<TEnvelope, string?> ValueProvider)
    //     where TEnvelope : IRawBrokerEnvelope;
    internal class LogAdditionalArgument<TEnvelope>
        where TEnvelope : IRawBrokerEnvelope
    {
        public LogAdditionalArgument(string name, Func<TEnvelope, string?> valueProvider)
        {
            Name = name;
            ValueProvider = valueProvider;
        }

        public string Name { get; }

        public Func<TEnvelope, string?> ValueProvider { get; }
    }
}
