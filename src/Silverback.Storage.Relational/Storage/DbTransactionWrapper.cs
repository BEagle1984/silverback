// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Storage;

// TODO: Test (equality too)
internal sealed class DbTransactionWrapper : IStorageTransaction, IEquatable<DbTransactionWrapper>
{
    private readonly DbTransaction _transaction;

    private readonly ISilverbackContext _context;

    public DbTransactionWrapper(DbTransaction transaction, ISilverbackContext context)
    {
        _transaction = Check.NotNull(transaction, nameof(transaction));
        _context = Check.NotNull(context, nameof(context));

        _context.EnlistTransaction(this);
    }

    public object UnderlyingTransaction => _transaction;

    public static bool operator ==(DbTransactionWrapper? left, DbTransactionWrapper? right) => Equals(left, right);

    public static bool operator !=(DbTransactionWrapper? left, DbTransactionWrapper? right) => !Equals(left, right);

    public bool Equals(DbTransactionWrapper? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return _transaction.Equals(other._transaction);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is DbTransactionWrapper other && Equals(other);

    public override int GetHashCode() => _transaction.GetHashCode();

    public void Commit() => _transaction.Commit();

    public Task CommitAsync() => _transaction.CommitAsync();

    public void Rollback() => _transaction.Rollback();

    public Task RollbackAsync() => _transaction.RollbackAsync();

    public void Dispose()
    {
        _context.RemoveTransaction();
        _transaction.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _context.RemoveTransaction();
        return _transaction.DisposeAsync();
    }
}
