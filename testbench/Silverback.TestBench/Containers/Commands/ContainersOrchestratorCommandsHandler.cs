// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.TestBench.Containers.Commands;

public class ContainersOrchestratorCommandsHandler
{
    private readonly ContainersOrchestrator _orchestrator;

    public ContainersOrchestratorCommandsHandler(ContainersOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by the framework")]
    public void Handle(ScaleInCommand command) => _orchestrator.ScaleIn();

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by the framework")]
    public void Handle(ScaleOutCommand command) => _orchestrator.ScaleOut();

    public void Handle(SetInstancesCommand command) => _orchestrator.SetInstances(command.Instances);
}
