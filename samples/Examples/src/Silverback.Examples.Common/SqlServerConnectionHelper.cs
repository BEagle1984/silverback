// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;

namespace Silverback.Examples.Common
{
    public static class SqlServerConnectionHelper
    {
        private const string ConnectionString =
            @"Data Source=.,1433;Initial Catalog=Silverback.Examples.{0};User ID=sa;Password=mssql2017.;";

        public static string GetProducerConnectionString() =>
            string.Format(CultureInfo.InvariantCulture, ConnectionString, "Producer");

        public static string GetConsumerConnectionString() =>
            string.Format(CultureInfo.InvariantCulture, ConnectionString, "Consumer");
    }
}
