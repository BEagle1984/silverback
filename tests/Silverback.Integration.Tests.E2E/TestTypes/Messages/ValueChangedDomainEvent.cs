// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;
using Silverback.Tests.Integration.E2E.TestTypes.Database;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class ValueChangedDomainEvent : DomainEvent<TestDomainEntity>
{
}
