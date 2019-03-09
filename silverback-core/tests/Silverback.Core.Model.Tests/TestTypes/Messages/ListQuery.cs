// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Silverback.Messaging.Messages;

namespace Silverback.Core.Model.Tests.TestTypes.Messages
{
    public class ListQuery : IQuery<IEnumerable<int>>
    {
        public int Count { get; set; }
    }
}