// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Database
{
    public class TestDomainEntity : DomainEntity
    {
        public int Id { get; private set; }

        public int Value { get; private set; }

        public void SetValue(int newValue)
        {
            Value = newValue;
            AddEvent<ValueChangedDomainEvent>();
        }
    }
}
