// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using System.Threading.Tasks;

namespace Silverback.Storage.DataAccess;

internal static class DbTransactionExtensions
{
    public static async Task CommitAndDisposeAsync(this DbTransaction transaction)
    {
        DbConnection? connection = transaction.Connection;
        await transaction.CommitAsync().ConfigureAwait(false);

        if (connection != null)
            await connection.CloseAndDisposeAsync().ConfigureAwait(false);

        await transaction.DisposeAsync().ConfigureAwait(false);
    }

    public static async Task RollbackAndDisposeAsync(this DbTransaction transaction)
    {
        DbConnection? connection = transaction.Connection;
        await transaction.RollbackAsync().ConfigureAwait(false);

        if (connection != null)
            await connection.CloseAndDisposeAsync().ConfigureAwait(false);

        await transaction.DisposeAsync().ConfigureAwait(false);
    }
}
