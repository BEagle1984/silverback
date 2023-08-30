// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Ductus.FluentDocker.Services;

namespace Silverback.TestBench.Containers;

public sealed record ContainerInstance(string ProjectName, IContainerService ContainerService);
