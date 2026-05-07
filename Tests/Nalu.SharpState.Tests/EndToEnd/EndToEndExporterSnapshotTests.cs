namespace Nalu.SharpState.Tests.EndToEnd;

[UsesVerify]
public class EndToEndExporterSnapshotTests
{
    [Fact]
    public Task Dot_DoorMachine_guard_labels()
        => Verify(DoorMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task Dot_ReactionMachine_terminal_states()
        => Verify(ReactionMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task Dot_NetworkMachine_hierarchy()
        => Verify(NetworkMachine.ToDot(), ExporterSnapshotTestSettings.Create());

    [Fact]
    public Task Mermaid_NetworkMachine()
        => Verify(NetworkMachine.ToMermaid(), ExporterSnapshotTestSettings.Create());
}
