namespace Nalu.SharpState.Tests;

internal static class ExporterSnapshotTestSettings
{
    /// <summary>
    /// Snapshots live under <c>Tests/Nalu.SharpState.Tests/Snapshots</c> (one level up from <c>Runtime/</c> or <c>EndToEnd/</c>).
    /// Files use the default <c>.verified.txt</c> extension; content is Graphviz DOT or Mermaid source.
    /// </summary>
    public static VerifySettings Create()
    {
        var settings = new VerifySettings();
        settings.UseDirectory("../Snapshots");
        return settings;
    }
}
