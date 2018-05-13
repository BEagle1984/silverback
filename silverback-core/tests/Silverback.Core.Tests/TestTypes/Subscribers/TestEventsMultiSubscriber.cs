using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestEventsMultiSubscriber : MultiSubscriber
    {
        public static int CounterCommandOne { get; set; }
        public static int CounterCommandTwo { get; set; }

        public static int CounterFiltered { get; set; }

        /// <summary>
        /// Configures the <see cref="T:Silverback.Messaging.MultiMessageHandler" /> binding the actual message handlers methods.
        /// </summary>
        /// <param name="config">The configuration.</param>
        protected override void Configure(MultiSubscriberConfig config)
        {
            config
                .AddAsyncHandler<TestEventOne>(OnEventOne)
                .AddHandler<TestEventTwo>(OnEventTwo)
                .AddHandler<TestEventOne>(OnEventOneFiltered, m => m.Message != "skip");
        }

        private async Task OnEventOne(TestEventOne message)
        {
            await Task.Delay(1);
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
