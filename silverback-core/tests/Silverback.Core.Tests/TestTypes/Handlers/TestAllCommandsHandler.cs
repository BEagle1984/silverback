using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Handlers
{
    public class TestAllEventsHandler : MultiMessageHandler
    {
        public static int CounterCommandOne { get; set; }
        public static int CounterCommandTwo { get; set; }

        public static int CounterFiltered { get; set; }

        /// <summary>
        /// Configures the <see cref="T:Silverback.Messaging.MultiMessageHandler" /> binding the actual message handlers methods.
        /// </summary>
        /// <param name="config">The configuration.</param>
        protected override void Configure(MultiMessageHandlerConfiguration config)
        {
            config
                .AddHandler<TestEventOne>(OnEventOne)
                .AddHandler<TestEventTwo>(OnEventTwo)
                .AddHandler<TestEventOne>(OnEventOneFiltered, m => m.Message != "skip");
        }

        private void OnEventOne(TestEventOne message)
        {
            CounterCommandOne++;
        }

        private void OnEventTwo(TestEventTwo message)
        {
            CounterCommandTwo++;
        }

        private void OnEventOneFiltered(TestEventOne message)
        {
            CounterFiltered++;
        }
    }
}
