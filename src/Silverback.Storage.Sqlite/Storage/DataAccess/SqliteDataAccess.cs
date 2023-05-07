// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Data.Sqlite;

namespace Silverback.Storage.DataAccess;

internal class SqliteDataAccess : DataAccess<SqliteConnection, SqliteTransaction, SqliteParameter>
{
    public SqliteDataAccess(string connectionString)
        : base(connectionString)
    {
    }

    protected override SqliteConnection CreateConnection(string connectionString) => new(connectionString);

    protected override SqliteParameter CreateParameterCore(string name, object value) => new(name, value);
}
