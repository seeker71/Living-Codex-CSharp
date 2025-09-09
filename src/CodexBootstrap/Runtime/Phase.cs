using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class PhaseEngine
{
    // Melt: promote to Water for editing; keep same id (mutable) but mark phase intent in Meta
    public Node Melt(Node n)
        => n with { State = ContentState.Water, Meta = UpsertMeta(n.Meta, "phase", "melt") };

    // Refreeze: freeze back to Ice after edits; add simple lineage stamp
    public Node Refreeze(Node n)
        => n with { State = ContentState.Ice, Meta = UpsertMeta(n.Meta, "phase", "ice") };

    private static Dictionary<string, object> UpsertMeta(Dictionary<string, object>? src, string k, object v)
    {
        var d = src is null ? new Dictionary<string, object>() : new Dictionary<string, object>(src);
        d[k] = v; return d;
    }
}

public interface IResonanceChecker
{
    Task<ResonanceReport> CheckAsync(ResonanceProposal proposal, NodeRegistry registry);
}

public sealed class TrivialResonanceChecker : IResonanceChecker
{
    // Minimal: always OK. Future coils: compare anchors' cache keys/types for compatibility.
    public Task<ResonanceReport> CheckAsync(ResonanceProposal proposal, NodeRegistry registry)
        => Task.FromResult(new ResonanceReport(true, "trivial-ok"));
}
