// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Performance.TestTypes
{
    public class WhetherForecast
    {
        public DateTimeOffset Date { get; set; }

        public int TemperatureCelsius { get; set; }

        public string? Summary { get; set; }

        public int WindSpeed { get; set; }
    }
}
