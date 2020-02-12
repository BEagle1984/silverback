// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Common
{
    public static class SqlServerConnectionHelper
    {
        private const string ConnectionString =
            @"Data Source=.,1433;Initial Catalog=Silverback.Examples.{0};User ID=sa;Password=mssql2017.;";

        public static string GetProducerConnectionString() =>
            string.Format(ConnectionString, "Main");

        public static string GetConsumerConnectionString(string dbNameSuffix) =>
            string.Format(ConnectionString, dbNameSuffix);
    }
}