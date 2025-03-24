// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.Containers.Commands;

public record StopContainerCommand(ContainerInstanceViewModel ContainerInstance) : ICommand;
