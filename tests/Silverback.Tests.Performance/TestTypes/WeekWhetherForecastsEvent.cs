// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Silverback.Tests.Performance.TestTypes
{
    public class WeekWhetherForecastsEvent
    {
        public static WeekWhetherForecastsEvent Sample { get; } = new WeekWhetherForecastsEvent
        {
            Monday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-06", CultureInfo.InvariantCulture),
                TemperatureCelsius = 10,
                Summary = "Cool",
                WindSpeed = 8
            },
            Tuesday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-07", CultureInfo.InvariantCulture),
                TemperatureCelsius = 11,
                Summary = "Rainy",
                WindSpeed = 10
            },
            Wednesday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-08", CultureInfo.InvariantCulture),
                TemperatureCelsius = 10,
                Summary = "Cool",
                WindSpeed = 8
            },
            Thursday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-09", CultureInfo.InvariantCulture),
                TemperatureCelsius = 11,
                Summary = "Rainy",
                WindSpeed = 10
            },
            Friday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-10", CultureInfo.InvariantCulture),
                TemperatureCelsius = 10,
                Summary = "Cool",
                WindSpeed = 8
            },
            Saturday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-11", CultureInfo.InvariantCulture),
                TemperatureCelsius = 11,
                Summary = "Rainy",
                WindSpeed = 10
            },
            Sunday = new WhetherForecast
            {
                Date = DateTime.Parse("2020-01-12", CultureInfo.InvariantCulture),
                TemperatureCelsius = 10,
                Summary = "Cool",
                WindSpeed = 8
            }
        };

        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public static byte[] SerializedSample { get; } = JsonSerializer.SerializeToUtf8Bytes(Sample);

        public WhetherForecast? Monday { get; set; }

        public WhetherForecast? Tuesday { get; set; }

        public WhetherForecast? Wednesday { get; set; }

        public WhetherForecast? Thursday { get; set; }

        public WhetherForecast? Friday { get; set; }

        public WhetherForecast? Saturday { get; set; }

        public WhetherForecast? Sunday { get; set; }
    }
}
