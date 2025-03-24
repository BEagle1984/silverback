// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.TestBench.Containers.Commands;

public record SetInstancesCommand(int Instances) : ICommand;
