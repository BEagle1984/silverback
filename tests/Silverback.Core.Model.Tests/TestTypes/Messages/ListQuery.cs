// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.Model.TestTypes.Messages
{
    public class ListQuery : IQuery<IEnumerable<int>>
    {
        public int Count { get; set; }
    }
}
