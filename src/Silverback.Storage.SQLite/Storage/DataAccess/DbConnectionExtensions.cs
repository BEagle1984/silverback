// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using System.Threading.Tasks;

namespace Silverback.Storage.DataAccess;

// TODO: Move to Silverback.Storage.RelationalDatabase
internal static class DbConnectionExtensions
{
    public static async Task CloseAndDisposeAsync(this DbConnection connection)
    {
        await connection.CloseAsync().ConfigureAwait(false);
        await connection.DisposeAsync().ConfigureAwait(false);
    }
}
