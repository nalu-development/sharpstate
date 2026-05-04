using System.Text;

namespace Nalu.SharpState;

/// <summary>
/// Renders frozen state-machine definitions as Graphviz DOT graphs.
/// Hierarchical regions are <c>subgraph</c> clusters; region-level internal (Stay) transitions use an invisible
/// anchor node inside the cluster, with the trigger node and its edges emitted after the cluster closing brace.
/// </summary>
public static class StateMachineDotExporter
{
    /// <summary>
    /// Converts a state-machine definition into a DOT graph.
    /// </summary>
    /// <param name="definition">The frozen state-machine definition to render.</param>
    /// <param name="rootInitialState">The initial state of the root region.</param>
    /// <param name="graphName">The label shown on the graph.</param>
    /// <typeparam name="TContext">Type of the machine context.</typeparam>
    /// <typeparam name="TState">Type of the state enum.</typeparam>
    /// <typeparam name="TTrigger">Type of the trigger enum.</typeparam>
    /// <typeparam name="TActor">Type of the actor passed to reactions.</typeparam>
    /// <returns>The DOT source for the graph.</returns>
    public static string ToDot<TContext, TState, TTrigger, TActor>(
        StateMachineDefinition<TContext, TState, TTrigger, TActor> definition,
        TState rootInitialState,
        string graphName)
        where TState : struct, Enum
        where TTrigger : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(graphName);

        var rootInitialLeaf = definition.LeafOf(rootInitialState);
        var states = definition.States.ToArray();
        var stateOrder = states
            .Select((state, index) => (state, index))
            .ToDictionary(pair => pair.state, pair => pair.index);
        var stateIds = states.ToDictionary(state => state, state => $"state_{stateOrder[state]}");
        var nestedInitialStates = new HashSet<TState>(definition.InitialChild.Values, EqualityComparer<TState>.Default);
        nestedInitialStates.Remove(rootInitialState);
        var childrenByParent = definition.Parent
            .GroupBy(pair => pair.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(pair => pair.Key)
                    .OrderBy(state => stateOrder[state])
                    .ToArray());
        var rootStates = states
            .Where(state => !definition.Parent.ContainsKey(state))
            .OrderBy(state => stateOrder[state])
            .ToArray();
        var triggers = Enum.GetValues<TTrigger>();
        var terminalStates = states
            .Where(IsTerminalState)
            .ToHashSet(EqualityComparer<TState>.Default);

        var builder = new StringBuilder();
        var emittedNodes = new HashSet<string>(StringComparer.Ordinal);
        var deferredEdges = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var clusterCounter = 0;
        var auxiliaryNodeCounter = 0;

        builder.AppendLine("digraph G {");
        builder.AppendLine($"  label = \"{Escape(graphName)}\";");
        builder.AppendLine("  labelloc = t;");
        builder.AppendLine("  compound = true;");
        builder.AppendLine($"  start [shape=Mdiamond,label=\"{Escape(rootInitialLeaf.ToString())}\"];");
        builder.AppendLine();
        EmitRootLeaves(rootStates, 1);
        EmitTransitions(rootInitialLeaf, 1, "start", currentContainerPath: []);
        builder.AppendLine();
        EmitClusters(rootStates, 1, []);
        FlushDeferredEdges([], 1);
        builder.AppendLine("}");
        return builder.ToString();

        void EmitRootLeaves(IEnumerable<TState> regionStates, int indent)
        {
            foreach (var state in regionStates)
            {
                if (childrenByParent.ContainsKey(state) || EqualityComparer<TState>.Default.Equals(state, rootInitialLeaf))
                {
                    continue;
                }

                EmitStateNode(state, indent);
                EmitTransitions(state, indent, stateIds[state], currentContainerPath: []);
            }
        }

        void EmitClusters(IEnumerable<TState> regionStates, int indent, IReadOnlyList<TState> parentContainerPath)
        {
            foreach (var state in regionStates)
            {
                if (!childrenByParent.TryGetValue(state, out var children))
                {
                    continue;
                }

                var clusterPath = parentContainerPath.Concat([state]).ToArray();
                // Graphviz cannot attach edges to a subgraph; use an invisible point node inside the cluster and emit
                // region-level Stay triggers and their edges after the cluster closes (still linking to that anchor).
                var clusterId = clusterCounter++;
                WriteLine(indent, $"subgraph cluster_{clusterId} {{");
                WriteLine(indent + 1, "style=rounded;");
                WriteLine(indent + 1, "color=lightgrey;");
                WriteLine(indent + 1, $"label = \"{Escape(state.ToString())}\";");
                var clusterAnchorId = $"cluster_{clusterId}_anchor";
                WriteLine(indent + 1, $"{clusterAnchorId} [shape=point, style=invis];");

                EmitCompositeChildren(children, indent + 1, clusterPath);
                var compositeStayOutsideSubgraph = new List<string>();
                EmitTransitions(
                    state,
                    indent + 1,
                    sourceNodeId: null,
                    currentContainerPath: clusterPath,
                    compositeClusterAnchorId: clusterAnchorId,
                    compositeStayOutsideSubgraph: compositeStayOutsideSubgraph);
                FlushDeferredEdges(clusterPath, indent + 1);

                WriteLine(indent, "}");
                foreach (var line in compositeStayOutsideSubgraph)
                {
                    WriteLine(indent, line);
                }
            }
        }

        void EmitCompositeChildren(IEnumerable<TState> children, int indent, IReadOnlyList<TState> currentContainerPath)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var child in children)
            {
                if (!childrenByParent.ContainsKey(child))
                {
                    EmitStateNode(child, indent);
                    EmitTransitions(child, indent, stateIds[child], currentContainerPath);
                }
            }

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var child in children)
            {
                if (childrenByParent.ContainsKey(child))
                {
                    EmitClusters([child], indent, currentContainerPath);
                }
            }
        }

        void EmitStateNode(TState state, int indent)
        {
            var stateId = stateIds[state];
            if (emittedNodes.Add(stateId))
            {
                var shape = terminalStates.Contains(state)
                    ? "Msquare"
                    : nestedInitialStates.Contains(state)
                        ? "Mdiamond"
                        : "rectangle";
                WriteLine(indent, $"{stateId} [shape={shape},label=\"{Escape(state.ToString())}\"];");
            }
        }

        void EmitTransitions(
            TState sourceState,
            int indent,
            string? sourceNodeId,
            IReadOnlyList<TState> currentContainerPath,
            string? compositeClusterAnchorId = null,
            List<string>? compositeStayOutsideSubgraph = null)
        {
            var configuration = definition.GetConfiguration(sourceState);

            foreach (var trigger in triggers)
            {
                if (!configuration.TryGetTransitions(trigger, out var transitions))
                {
                    continue;
                }

                foreach (var transition in transitions)
                {
                    if (transition.TargetSelector is not null
                        && transition.DynamicTargetStates is { Length: > 0 } hintTargets)
                    {
                        var hintTriggerId = NextAuxiliaryId("trigger");
                        WriteLine(indent, $"{hintTriggerId} [shape=ellipse,label=\"{Escape(BuildTriggerLabel(trigger, transition))}\"];");
                        if (sourceNodeId is not null)
                        {
                            WriteLine(indent, $"{sourceNodeId} -> {hintTriggerId};");
                        }

                        var seenTargetNodes = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var hintState in hintTargets)
                        {
                            var leaf = definition.LeafOf(hintState);
                            var nodeId = NodeIdFor(leaf);
                            if (!seenTargetNodes.Add(nodeId))
                            {
                                continue;
                            }

                            var scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(leaf));
                            EnqueueDeferredEdge(scopePath, $"{hintTriggerId} -> {nodeId};");
                        }

                        continue;
                    }

                    var targetNodeId = ResolveTargetNodeId(sourceState, transition, indent, compositeClusterAnchorId);
                    if (sourceNodeId is null && targetNodeId is null)
                    {
                        continue;
                    }

                    var triggerId = NextAuxiliaryId("trigger");

                    var isCompositeRegionStay = transition.IsInternal
                        && childrenByParent.ContainsKey(sourceState)
                        && compositeClusterAnchorId is not null
                        && compositeStayOutsideSubgraph is not null
                        && targetNodeId is not null
                        && string.Equals(targetNodeId, compositeClusterAnchorId, StringComparison.Ordinal);

                    if (isCompositeRegionStay)
                    {
                        var outside = compositeStayOutsideSubgraph!;
                        var anchorId = targetNodeId!;
                        outside.Add($"{triggerId} [shape=ellipse,label=\"{Escape(BuildTriggerLabel(trigger, transition))}\"];");
                        outside.Add($"{triggerId} -> {anchorId};");
                        outside.Add($"{anchorId} -> {triggerId};");
                        continue;
                    }

                    WriteLine(indent, $"{triggerId} [shape=ellipse,label=\"{Escape(BuildTriggerLabel(trigger, transition))}\"];");
                    if (sourceNodeId is not null)
                    {
                        WriteLine(indent, $"{sourceNodeId} -> {triggerId};");
                    }

                    if (targetNodeId is not null)
                    {
                        DeferTargetEdge(currentContainerPath, transition, $"{triggerId} -> {targetNodeId};");
                    }
                }
            }
        }

        string? ResolveTargetNodeId(
            TState sourceState,
            Transition<TContext, TState, TActor> transition,
            int indent,
            string? compositeClusterAnchorId)
        {
            if (transition.IsInternal)
            {
                if (childrenByParent.ContainsKey(sourceState))
                {
                    return compositeClusterAnchorId;
                }

                return NodeIdFor(sourceState);
            }

            if (transition.TargetSelector is not null)
            {
                var dynamicTargetId = NextAuxiliaryId("dynamic_target");
                WriteLine(indent, $"{dynamicTargetId} [shape=rectangle,label=\"Dynamic target\"];");
                return dynamicTargetId;
            }

            return NodeIdFor(definition.LeafOf(transition.Target));
        }

        string NextAuxiliaryId(string prefix) => $"{prefix}_{auxiliaryNodeCounter++}";

        void EnqueueDeferredEdge(IReadOnlyList<TState> scopePath, string edge)
        {
            var key = ScopeKey(scopePath);
            if (!deferredEdges.TryGetValue(key, out var edges))
            {
                edges = [];
                deferredEdges[key] = edges;
            }

            edges.Add(edge);
        }

        void DeferTargetEdge(
            IReadOnlyList<TState> currentContainerPath,
            Transition<TContext, TState, TActor> transition,
            string edge)
        {
            IReadOnlyList<TState> scopePath;
            if (transition.TargetSelector is not null || transition.IsInternal)
            {
                scopePath = currentContainerPath;
            }
            else
            {
                var targetLeaf = definition.LeafOf(transition.Target);
                scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(targetLeaf));
            }

            EnqueueDeferredEdge(scopePath, edge);
        }

        IReadOnlyList<TState> LowestCommonContainerPath(IReadOnlyList<TState> sourcePath, IReadOnlyList<TState> targetPath)
        {
            var comparer = EqualityComparer<TState>.Default;
            var length = Math.Min(sourcePath.Count, targetPath.Count);
            var result = new List<TState>(length);
            for (var i = 0; i < length; i++)
            {
                if (!comparer.Equals(sourcePath[i], targetPath[i]))
                {
                    break;
                }

                result.Add(sourcePath[i]);
            }

            return result;
        }

        void FlushDeferredEdges(IReadOnlyList<TState> scopePath, int indent)
        {
            var key = ScopeKey(scopePath);
            if (!deferredEdges.TryGetValue(key, out var edges))
            {
                return;
            }

            foreach (var edge in edges)
            {
                WriteLine(indent, edge);
            }
        }

        List<TState> GetVisualContainerPath(TState state)
        {
            var path = definition.AncestorsOf(state).Reverse().ToList();
            if (childrenByParent.ContainsKey(state))
            {
                path.Add(state);
            }

            return path;
        }

        bool IsTerminalState(TState state)
        {
            var configuration = definition.GetConfiguration(state);
            foreach (var trigger in triggers)
            {
                if (configuration.TryGetTransitions(trigger, out _))
                {
                    return false;
                }
            }

            return true;
        }

        string NodeIdFor(TState state)
            => EqualityComparer<TState>.Default.Equals(state, rootInitialLeaf)
                ? "start"
                : stateIds[state];

        string ScopeKey(IReadOnlyList<TState> scopePath)
            => scopePath.Count == 0
                ? string.Empty
                : string.Join("|", scopePath.Select(state => stateOrder[state]));

        string BuildTriggerLabel(TTrigger trigger, Transition<TContext, TState, TActor> transition)
        {
            var triggerName = trigger.ToString();
            if (transition.Guard is null)
            {
                return triggerName;
            }

            var unnamedGuardCounter = 0;
            var guardBracket = transition.GuardLabels is null
                ? $"Unnamed guard {++unnamedGuardCounter}"
                : string.Join(" & ", transition.GuardLabels);
            return $"{triggerName}\n[{guardBracket}]";
        }

        void WriteLine(int indent, string text)
            => builder.Append(' ', indent * 2).AppendLine(text);
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n");
}
