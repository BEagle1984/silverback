// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.Rx.TestTypes.Messages.Base
{
    public interface IQuery<out TResult> : IRequest<TResult>
    {
    }
}