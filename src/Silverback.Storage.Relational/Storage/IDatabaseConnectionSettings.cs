// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Storage;

/// <summary>
///     The basic settings of the database connection.
/// </summary>
public interface IDatabaseConnectionSettings
{
    /// <summary>
    ///     Gets the connection string to the database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    ///     Gets the name of the Kafka offset store table.
    /// </summary>
    string TableName { get; init; }

    /// <summary>
    ///     Gets the database command timeout.
    /// </summary>
    TimeSpan DbCommandTimeout { get; init; }

    /// <summary>
    ///     Gets the timeout for the table creation.
    /// </summary>
    TimeSpan CreateTableTimeout { get; init; }
}
