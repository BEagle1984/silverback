// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class ScopeIdentifier
    {
        public Guid ScopeId { get; } = Guid.NewGuid();
    }
}
