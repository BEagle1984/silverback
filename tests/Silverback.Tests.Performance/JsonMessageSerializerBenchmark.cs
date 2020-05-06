using System;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;

namespace Silverback.Tests.Performance
{
    [MemoryDiagnoser]
    public class JsonMessageSerializerBenchmark
    {
        private readonly JsonMessageSerializer _serializer = new JsonMessageSerializer();

        private readonly MessageHeaderCollection _messageHeaderCollection = new MessageHeaderCollection();

        private readonly MessageSerializationContext _messageSerializationContext =
            new MessageSerializationContext(new TestProducerEndpoint("Name"));

        private readonly Forecasts _forecasts = new Forecasts
        {
            Monday = new Forecast
            {
                Date = DateTime.Parse("2020-01-06"),
                TemperatureCelsius = 10,
                Summary = "Cool",
                WindSpeed = 8
            },
            Tuesday = new Forecast
            {
                Date = DateTime.Parse("2020-01-07"),
                TemperatureCelsius = 11,
                Summary = "Rainy",
                WindSpeed = 10
            }
        };

        [Benchmark]
        public void Serialize()
        {
            for (int i = 0; i < 5; i++)
            {
                _serializer.Serialize(_forecasts, _messageHeaderCollection, _messageSerializationContext);
            }
        }

        public interface IForecast
        {
            public DateTimeOffset Date { get; set; }

            public int TemperatureCelsius { get; set; }

            public string Summary { get; set; }
        }

        public class Forecast : IForecast
        {
            public DateTimeOffset Date { get; set; }

            public int TemperatureCelsius { get; set; }

            public string Summary { get; set; }

            public int WindSpeed { get; set; }
        }

        public class Forecasts
        {
            public IForecast Monday { get; set; }

            public object Tuesday { get; set; }
        }
    }
}
