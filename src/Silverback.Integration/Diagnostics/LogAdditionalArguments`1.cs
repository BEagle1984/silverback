// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    internal class LogAdditionalArguments<TEnvelope>
        where TEnvelope : IRawBrokerEnvelope
    {
        public LogAdditionalArguments(
            string name1,
            Func<TEnvelope, string?> valueProvider1,
            string name2,
            Func<TEnvelope, string?> valueProvider2)
        {
            Argument1 = new LogAdditionalArgument<TEnvelope>(name1, valueProvider1);
            Argument2 = new LogAdditionalArgument<TEnvelope>(name2, valueProvider2);
        }

        public LogAdditionalArgument<TEnvelope> Argument1 { get; }

        public LogAdditionalArgument<TEnvelope> Argument2 { get; }

        public string EnrichMessage(string message) =>
            $"{message}, {Argument1.Name}: {{{Argument1.Name}}}, {Argument2.Name}: {{{Argument2.Name}}}";
    }
}
