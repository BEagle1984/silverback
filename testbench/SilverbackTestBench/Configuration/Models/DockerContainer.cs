// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.TestBench.Configuration.Models;

public record DockerContainer(string ProjectName, string ImageName, string ImageTag, int DefaultInstances);
