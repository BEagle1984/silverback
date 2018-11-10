// TODO: Delete?

//using System;
//using System.Threading.Tasks;
//using Silverback.Messaging.Subscribers;
//using Silverback.Tests.TestTypes.Domain;

//namespace Silverback.Tests.TestTypes.Subscribers
//{
//    public class TestEventsMultiSubscriber : MultiSubscriber
//    {
//        public int HandledEventOne { get; set; }
//        public int HandledEventTwo { get; set; }
//        public int HandledFilteredEventOne { get; set; }
//        public int HandledFilteredEventTwo { get; set; }

//        /// <summary>
//        /// Configures the <see cref="T:Silverback.Messaging.MultiMessageHandler" /> binding the actual message handlers methods.
//        /// </summary>
//        /// <param name="config">The configuration.</param>
//        protected override void Configure(MultiSubscriberConfig config)
//        {
//            config
//                .AddAsyncHandler<TestEventOne>(OnEventOne)
//                .AddHandler<TestEventTwo>(OnEventTwo)
//                .AddHandler<TestEventOne>(OnEventOneFiltered, m => m.Message == "yes")
//                .AddAsyncHandler<TestEventTwo>(OnEventTwoFiltered, m => m.Message == "yes");
//        }

//        private async Task OnEventOne(TestEventOne message)
//        {
//            await Task.Delay(1);
//            HandledEventOne++;
//        }

//        private void OnEventTwo(TestEventTwo message)
//        {
//            HandledEventTwo++;
//        }

//        private void OnEventOneFiltered(TestEventOne message)
//        {
//            HandledFilteredEventOne++;
//        }

//        private async Task OnEventTwoFiltered(TestEventTwo message)
//        {
//            await Task.Delay(1);
//            HandledFilteredEventTwo++;
//        }
//    }
//}
