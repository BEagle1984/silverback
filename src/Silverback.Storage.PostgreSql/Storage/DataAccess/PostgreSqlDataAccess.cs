// Copyright (c) 2023 Sergio Aquilini
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

    protected override NpgsqlParameter CreateParameterCore(string name, object value) => new(name, value);
}