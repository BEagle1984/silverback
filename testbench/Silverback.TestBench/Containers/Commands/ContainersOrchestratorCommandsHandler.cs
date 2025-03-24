// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.TestBench.Containers.Commands;

[SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Intentional fire-and-forget to avoid blocking the UI")]
public class ContainersOrchestratorCommandsHandler
{
    private readonly ContainersOrchestrator _orchestrator;

    public ContainersOrchestratorCommandsHandler(ContainersOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by the framework")]
    public void Handle(ScaleInCommand command) => Task.Run(_orchestrator.ScaleIn);

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by the framework")]
    public void Handle(ScaleOutCommand command) => Task.Run(_orchestrator.ScaleOut);

    public void Handle(SetInstancesCommand command) => Task.Run(() => _orchestrator.SetInstances(command.Instances));
}
