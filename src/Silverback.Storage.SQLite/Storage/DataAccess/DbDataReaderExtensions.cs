// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;

namespace Silverback.Storage.DataAccess;

// TODO: Move to Silverback.Storage.RelationalDatabase
internal static class DbDataReaderExtensions
{
    public static T? GetNullableFieldValue<T>(this DbDataReader reader, int ordinal)
        where T : class =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
}
