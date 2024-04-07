// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Npgsql;

namespace Silverback.Storage.DataAccess;

internal class PostgreSqlDataAccess : DataAccess<NpgsqlConnection, NpgsqlTransaction, NpgsqlParameter>
{
    public PostgreSqlDataAccess(string connectionString)
        : base(connectionString)
    {
    }

    protected override NpgsqlConnection CreateConnection(string connectionString) => new(connectionString);
}
