// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.TestBench.ViewModel.Containers;

namespace Silverback.Tests.Extended.TestBench.Containers.Commands;

public record StopContainerCommand(ContainerInstanceViewModel ContainerInstance) : ICommand;
