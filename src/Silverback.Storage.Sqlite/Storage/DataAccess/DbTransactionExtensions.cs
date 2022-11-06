// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using System.Threading.Tasks;

namespace Silverback.Storage.DataAccess;

// TODO: Test
internal static class DbTransactionExtensions
{
    public static void CommitAndDispose(this DbTransaction transaction)
    {
        DbConnection? connection = transaction.Connection;
        transaction.Commit();

        if (connection != null)
            connection.CloseAndDispose();

        transaction.Dispose();
    }

    public static async Task CommitAndDisposeAsync(this DbTransaction transaction)
    {
        DbConnection? connection = transaction.Connection;
        await transaction.CommitAsync().ConfigureAwait(false);

        if (connection != null)
            await connection.CloseAndDisposeAsync().ConfigureAwait(false);

        await transaction.DisposeAsync().ConfigureAwait(false);
    }

    public static void RollbackAndDispose(this DbTransaction transaction)
    {
        DbConnection? connection = transaction.Connection;
        transaction.Rollback();

        if (connection != null)
            connection.CloseAndDispose();

        transaction.Dispose();
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
