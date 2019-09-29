// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.EFCore30.TestTypes.Base
{
    public interface IQuery<out TResult> : IRequest<TResult>
    {
    }
}