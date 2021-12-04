// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Data.Sqlite;

namespace Silverback.Tests;

public static class SqliteConnectionExtensions
{
    /// <summary>
    ///     Safely closes the connection. This is a workaround for the random exceptions
    ///     thrown by the <c>Close</c> or <c>Dispose</c> methods when using the in memory database.
    /// </summary>
    /// <param name="connection">
    ///     The <see cref="SqliteConnection" /> to be closed.
    /// </param>
    public static void SafeClose(this SqliteConnection connection)
    {
        try
        {
            connection.Close();
        }
        catch (Exception)
        {
            // Ignore
        }

        try
        {
            connection.Dispose();
        }
        catch (Exception)
        {
            // Ignore
        }
    }
}
