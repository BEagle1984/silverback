// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.EFCore22.TestTypes.Base
{
    public interface IQuery<out TResult> : IRequest<TResult>
    {
    }
}