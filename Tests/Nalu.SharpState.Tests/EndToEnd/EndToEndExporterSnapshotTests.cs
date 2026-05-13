namespace Nalu.SharpState.Tests.EndToEnd;

[UsesVerify]
public class EndToEndExporterSnapshotTests
{
    [Fact]
    public Task DotDoorMachineGuardLabels()
        => Verify(DoorMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task DotReactionMachineTerminalStates()
        => Verify(ReactionMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task DotNetworkMachineHierarchy()
        => Verify(NetworkMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task MermaidNetworkMachine()
        => Verify(NetworkMachine.ToMermaid(), ExporterSnapshotTestSettings.Create());
}
