using System.Text;

namespace Nalu.SharpState;

/// <summary>
/// Renders frozen state-machine definitions as graph sources.
/// Graphviz DOT hierarchical regions are <c>subgraph</c> clusters; region-level internal (Stay) transitions use an
/// invisible anchor node inside the cluster, with the trigger node and its edges emitted after the cluster closing brace.
/// </summary>
public static class StateMachineExporter
{
    /// <summary>
    /// Converts a state-machine definition into a DOT graph.
    /// </summary>
    /// <param name="definition">The frozen state-machine definition to render.</param>
    /// <param name="rootInitialState">The initial state of the root region.</param>
    /// <param name="graphName">The label shown on the graph.</param>
    /// <typeparam name="TContext">Type of the machine context.</typeparam>
    /// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
    /// <typeparam name="TState">Type of the state enum.</typeparam>
    /// <typeparam name="TTrigger">Type of the trigger enum.</typeparam>
    /// <typeparam name="TActor">Type of the actor passed to reactions.</typeparam>
    /// <returns>The DOT source for the graph.</returns>
    public static string ToDot<TContext, TArgs, TState, TTrigger, TActor>(
        StateMachineDefinition<TContext, TArgs, TState, TTrigger, TActor> definition,
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

                WriteLine(indent + 1, $"{clusterAnchorId} [shape=point, style=invis];");

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
                        && transition.DynamicTargetHints is { Length: > 0 } hintTargets)
                    {
                        var hintTriggerId = NextAuxiliaryId("trigger");
                        WriteLine(indent, $"{hintTriggerId} [shape=ellipse,label=\"{Escape(BuildTriggerLabel(trigger, transition))}\"];");
                        if (sourceNodeId is not null)
                        {
                            WriteLine(indent, $"{sourceNodeId} -> {hintTriggerId};");
                        }

                        var seenTargetEdges = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var hint in hintTargets)
                        {
                            var leaf = definition.LeafOf(hint.Target);
                            var nodeId = NodeIdFor(leaf);
                            var edge = BuildLabeledDotEdge(hintTriggerId, nodeId, hint.Label);
                            if (!seenTargetEdges.Add(edge))
                            {
                                continue;
                            }

                            var scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(leaf));
                            EnqueueDeferredEdge(scopePath, edge);
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
            Transition<TContext, TArgs, TState, TActor> transition,
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

        string BuildLabeledDotEdge(string sourceNodeId, string targetNodeId, string label)
            => string.IsNullOrWhiteSpace(label)
                ? $"{sourceNodeId} -> {targetNodeId};"
                : $"{sourceNodeId} -> {targetNodeId} [label=\"[{Escape(label)}]\"];";

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
            Transition<TContext, TArgs, TState, TActor> transition,
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

        string BuildTriggerLabel(TTrigger trigger, Transition<TContext, TArgs, TState, TActor> transition)
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

    /// <summary>
    /// Converts a state-machine definition into a Mermaid state diagram.
    /// </summary>
    /// <param name="definition">The frozen state-machine definition to render.</param>
    /// <param name="rootInitialState">The initial state of the root region.</param>
    /// <param name="graphName">The title shown on the diagram.</param>
    /// <typeparam name="TContext">Type of the machine context.</typeparam>
    /// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
    /// <typeparam name="TState">Type of the state enum.</typeparam>
    /// <typeparam name="TTrigger">Type of the trigger enum.</typeparam>
    /// <typeparam name="TActor">Type of the actor passed to reactions.</typeparam>
    /// <returns>The Mermaid state diagram source for the graph.</returns>
    public static string ToMermaid<TContext, TArgs, TState, TTrigger, TActor>(
        StateMachineDefinition<TContext, TArgs, TState, TTrigger, TActor> definition,
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

        var builder = new StringBuilder();
        var emittedStates = new HashSet<string>(StringComparer.Ordinal);
        var deferredTransitions = new Dictionary<string, List<MermaidTransition>>(StringComparer.Ordinal);
        var auxiliaryNodeCounter = 0;

        builder.AppendLine("---");
        builder.AppendLine($"title: \"{EscapeMermaidYamlString(graphName)}\"");
        builder.AppendLine("---");
        builder.AppendLine("%%{init: {\"layout\": \"elk\"}}%%");
        builder.AppendLine("stateDiagram-v2");
        builder.AppendLine();

        EmitStates(rootStates, 1, []);
        WriteLine(1, $"[*] --> {NodeIdFor(rootInitialLeaf)}");
        EmitTransitionsForRegion(rootStates, 1, []);
        FlushDeferredTransitions([], 1);

        return builder.ToString();

        void EmitStates(IEnumerable<TState> regionStates, int indent, IReadOnlyList<TState> currentContainerPath)
        {
            foreach (var state in regionStates)
            {
                if (childrenByParent.TryGetValue(state, out var children))
                {
                    var stateId = stateIds[state];
                    EmitStateDeclaration(state, indent);
                    WriteLine(indent, $"state {stateId} {{");

                    var initialLeaf = definition.LeafOf(definition.InitialChild[state]);
                    WriteLine(indent + 1, $"[*] --> {NodeIdFor(initialLeaf)}");

                    var containerPath = currentContainerPath.Concat([state]).ToArray();
                    var compositeStayOutsideDeclarations = new List<string>();
                    var compositeStayOutsideBlock = new List<MermaidTransition>();
                    EmitStates(children, indent + 1, containerPath);
                    EmitTransitionsForRegion(children, indent + 1, containerPath);
                    EmitTransitions(state, indent + 1, stateId, containerPath, compositeStayOutsideDeclarations, compositeStayOutsideBlock);
                    FlushDeferredTransitions(containerPath, indent + 1);

                    WriteLine(indent, "}");
                    foreach (var line in compositeStayOutsideDeclarations)
                    {
                        WriteLine(indent, line);
                    }

                    foreach (var line in CoalesceMermaidTransitions(compositeStayOutsideBlock))
                    {
                        WriteLine(indent, line);
                    }

                    continue;
                }

                EmitStateDeclaration(state, indent);
            }
        }

        void EmitStateDeclaration(TState state, int indent)
        {
            var stateId = stateIds[state];
            if (emittedStates.Add(stateId))
            {
                WriteLine(indent, $"state \"{EscapeMermaidQuotedText(state.ToString())}\" as {stateId}");
            }
        }

        void EmitTransitionsForRegion(IEnumerable<TState> regionStates, int indent, IReadOnlyList<TState> currentContainerPath)
        {
            foreach (var state in regionStates)
            {
                if (childrenByParent.ContainsKey(state))
                {
                    continue;
                }

                EmitTransitions(state, indent, NodeIdFor(state), currentContainerPath, null, null);
            }
        }

        void EmitTransitions(
            TState sourceState,
            int indent,
            string sourceNodeId,
            IReadOnlyList<TState> currentContainerPath,
            List<string>? compositeStayOutsideDeclarations,
            List<MermaidTransition>? compositeStayOutsideBlock)
        {
            var configuration = definition.GetConfiguration(sourceState);

            foreach (var trigger in triggers)
            {
                if (!configuration.TryGetTransitions(trigger, out var transitions))
                {
                    continue;
                }

                EscapeMermaidText(trigger.ToString());
                if (transitions.Any(transition => transition.Guard is not null))
                {
                    var choiceId = NextAuxiliaryId("choice");
                    var deferChoiceOutsideBlock = ShouldDeferCompositeTransitionOutsideBlock(sourceState, compositeStayOutsideBlock);
                    if (deferChoiceOutsideBlock)
                    {
                        compositeStayOutsideDeclarations!.Add(MermaidChoiceStateDeclaration(choiceId));
                        compositeStayOutsideBlock!.Add(new MermaidTransition(sourceNodeId, choiceId, trigger.ToString()));
                    }
                    else
                    {
                        WriteChoiceState(indent, choiceId);
                        WriteMermaidTransition(indent, new MermaidTransition(sourceNodeId, choiceId, trigger.ToString()));
                    }

                    foreach (var transition in transitions)
                    {
                        EmitTransitionTargets(
                            sourceState,
                            transition,
                            choiceId,
                            currentContainerPath,
                            indent,
                            ChoiceEdgeLabelFor(transition),
                            compositeStayOutsideBlock,
                            deferChoiceOutsideBlock);
                    }

                    continue;
                }

                foreach (var transition in transitions)
                {
                    if (transition.TargetSelector is not null
                        && transition.DynamicTargetHints is { Length: > 0 } hintTargets)
                    {
                        var choiceId = NextAuxiliaryId("choice");
                        WriteChoiceState(indent, choiceId);
                        WriteMermaidTransition(indent, new MermaidTransition(sourceNodeId, choiceId, trigger.ToString()));

                        var seenTargetEdges = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var hint in hintTargets)
                        {
                            var visualTarget = MermaidVisualTargetState(hint.Target);
                            var nodeId = NodeIdFor(visualTarget);
                            var hintTransition = new MermaidTransition(choiceId, nodeId, DynamicHintEdgeLabel(hint.Label));
                            if (!seenTargetEdges.Add(hintTransition.Identity))
                            {
                                continue;
                            }

                            var scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(visualTarget));
                            EnqueueDeferredTransition(scopePath, hintTransition);
                        }

                        continue;
                    }

                    var targetNodeId = ResolveTargetNodeId(sourceState, transition, indent);
                    var mermaidTransition = new MermaidTransition(sourceNodeId, targetNodeId, trigger.ToString());
                    if (TryDeferCompositeRegionStayOutsideBlock(
                            sourceState,
                            transition,
                            sourceNodeId,
                            targetNodeId,
                            mermaidTransition,
                            compositeStayOutsideBlock))
                    {
                        continue;
                    }

                    DeferTargetTransition(currentContainerPath, transition, mermaidTransition);
                }
            }
        }

        bool TryDeferCompositeRegionStayOutsideBlock(
            TState sourceState,
            Transition<TContext, TArgs, TState, TActor> transition,
            string sourceNodeId,
            string targetNodeId,
            MermaidTransition mermaidTransition,
            List<MermaidTransition>? compositeStayOutsideBlock)
        {
            if (compositeStayOutsideBlock is null
                || !transition.IsInternal
                || !childrenByParent.ContainsKey(sourceState)
                || !string.Equals(sourceNodeId, targetNodeId, StringComparison.Ordinal))
            {
                return false;
            }

            compositeStayOutsideBlock.Add(mermaidTransition);
            return true;
        }

        bool ShouldDeferCompositeTransitionOutsideBlock(
            TState sourceState,
            List<MermaidTransition>? compositeStayOutsideBlock)
            => compositeStayOutsideBlock is not null && childrenByParent.ContainsKey(sourceState);

        void EmitTransitionTargets(
            TState sourceState,
            Transition<TContext, TArgs, TState, TActor> transition,
            string sourceNodeId,
            IReadOnlyList<TState> currentContainerPath,
            int indent,
            string? edgeLabel,
            List<MermaidTransition>? compositeStayOutsideBlock,
            bool forceOutsideBlock = false)
        {
            if (transition.TargetSelector is not null
                && transition.DynamicTargetHints is { Length: > 0 } hintTargets)
            {
                var seenTargetEdges = new HashSet<string>(StringComparer.Ordinal);
                foreach (var hint in hintTargets)
                {
                    var visualTarget = MermaidVisualTargetState(hint.Target);
                    var nodeId = NodeIdFor(visualTarget);
                    var hintTransition = new MermaidTransition(
                        sourceNodeId,
                        nodeId,
                        CombineEdgeLabels(edgeLabel, DynamicHintEdgeLabel(hint.Label)));
                    if (!seenTargetEdges.Add(hintTransition.Identity))
                    {
                        continue;
                    }

                    if (forceOutsideBlock)
                    {
                        compositeStayOutsideBlock!.Add(hintTransition);
                    }
                    else
                    {
                        var scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(visualTarget));
                        EnqueueDeferredTransition(scopePath, hintTransition);
                    }
                }

                return;
            }

            var targetNodeId = ResolveTargetNodeId(sourceState, transition, indent);
            var mermaidTransition = new MermaidTransition(sourceNodeId, targetNodeId, edgeLabel);
            if (forceOutsideBlock)
            {
                compositeStayOutsideBlock!.Add(mermaidTransition);
                return;
            }

            if (TryDeferCompositeRegionStayOutsideBlock(
                    sourceState,
                    transition,
                    sourceNodeId,
                    targetNodeId,
                    mermaidTransition,
                    compositeStayOutsideBlock))
            {
                return;
            }

            DeferTargetTransition(
                currentContainerPath,
                transition,
                mermaidTransition);
        }

        // Mermaid edges to a composite target attach to that region's node, not the runtime initial leaf.
        TState MermaidVisualTargetState(TState target)
            => childrenByParent.ContainsKey(target) ? target : definition.LeafOf(target);

        string ResolveTargetNodeId(
            TState sourceState,
            Transition<TContext, TArgs, TState, TActor> transition,
            int indent)
        {
            if (transition.IsInternal)
            {
                return NodeIdFor(sourceState);
            }

            if (transition.TargetSelector is not null)
            {
                var dynamicTargetId = NextAuxiliaryId("dynamic_target");
                WriteLine(indent, $"state \"Dynamic target\" as {dynamicTargetId}");
                return dynamicTargetId;
            }

            return NodeIdFor(MermaidVisualTargetState(transition.Target));
        }

        string NextAuxiliaryId(string prefix) => $"{prefix}_{auxiliaryNodeCounter++}";

        void EnqueueDeferredTransition(IReadOnlyList<TState> scopePath, MermaidTransition transition)
        {
            var key = ScopeKey(scopePath);
            if (!deferredTransitions.TryGetValue(key, out var transitions))
            {
                transitions = [];
                deferredTransitions[key] = transitions;
            }

            transitions.Add(transition);
        }

        void DeferTargetTransition(
            IReadOnlyList<TState> currentContainerPath,
            Transition<TContext, TArgs, TState, TActor> transition,
            MermaidTransition mermaidTransition)
        {
            IReadOnlyList<TState> scopePath;
            if (transition.TargetSelector is not null || transition.IsInternal)
            {
                scopePath = currentContainerPath;
            }
            else
            {
                var visualTarget = MermaidVisualTargetState(transition.Target);
                scopePath = LowestCommonContainerPath(currentContainerPath, GetVisualContainerPath(visualTarget));
            }

            EnqueueDeferredTransition(scopePath, mermaidTransition);
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

        void FlushDeferredTransitions(IReadOnlyList<TState> scopePath, int indent)
        {
            var key = ScopeKey(scopePath);
            if (!deferredTransitions.TryGetValue(key, out var transitions))
            {
                return;
            }

            foreach (var transition in CoalesceMermaidTransitions(transitions))
            {
                WriteLine(indent, transition);
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

        string NodeIdFor(TState state) => stateIds[state];

        string ScopeKey(IReadOnlyList<TState> scopePath)
            => scopePath.Count == 0
                ? string.Empty
                : string.Join("|", scopePath.Select(state => stateOrder[state]));

        void WriteChoiceState(int indent, string choiceId)
            => WriteLine(indent, MermaidChoiceStateDeclaration(choiceId));

        string MermaidChoiceStateDeclaration(string choiceId) => $"state {choiceId} <<choice>>";

        void WriteMermaidTransition(int indent, MermaidTransition transition)
            => WriteLine(indent, BuildTransitionLine(transition.SourceNodeId, transition.TargetNodeId, transition.Label));

        IEnumerable<string> CoalesceMermaidTransitions(IEnumerable<MermaidTransition> transitions)
            => transitions
                .GroupBy(transition => transition.Identity, StringComparer.Ordinal)
                .Select(group =>
                {
                    var first = group.First();
                    var labels = group
                        .Select(transition => transition.Label)
                        .OfType<string>()
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                    return BuildTransitionLine(first.SourceNodeId, first.TargetNodeId, labels.Length == 0 ? null : string.Join(" / ", labels));
                });

        string BuildTransitionLine(string sourceNodeId, string targetNodeId, string? edgeLabel)
            => edgeLabel is null
                ? $"{sourceNodeId} --> {targetNodeId}"
                : $"{sourceNodeId} --> {targetNodeId} : {EscapeMermaidText(edgeLabel)}";

        string? DynamicHintEdgeLabel(string label)
            => string.IsNullOrWhiteSpace(label) ? null : $"[{label}]";

        string? CombineEdgeLabels(string? first, string? second)
            => (first, second) switch
            {
                (null, null) => null,
                ({ } value, null) => value,
                (null, { } value) => value,
                ({ } left, { } right) => $"{left} {right}"
            };

        string ChoiceEdgeLabelFor(Transition<TContext, TArgs, TState, TActor> transition)
        {
            if (transition.Guard is null)
            {
                return "[Else]";
            }

            var unnamedGuardCounter = 0;
            var guardLabel = transition.GuardLabels is null
                ? $"Unnamed guard {++unnamedGuardCounter}"
                : string.Join(" & ", transition.GuardLabels);
            return $"[{guardLabel}]";
        }

        void WriteLine(int indent, string text)
            => builder.Append(' ', indent * 2).AppendLine(text);
    }

    private readonly record struct MermaidTransition(string SourceNodeId, string TargetNodeId, string? Label)
    {
        public string Identity => $"{SourceNodeId}\u001f{TargetNodeId}";
    }

    private static string EscapeMermaidQuotedText(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string EscapeMermaidText(string value)
        => value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal);

    private static string EscapeMermaidYamlString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
