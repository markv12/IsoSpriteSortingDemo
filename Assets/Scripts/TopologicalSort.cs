using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TopologicalSort {
    private static readonly HashSet<int> visited = new HashSet<int>();
    private static readonly Dictionary<int, bool> circularDepData = new Dictionary<int, bool>();
    private static readonly List<IsoSpriteSorting> circularDepStack = new List<IsoSpriteSorting>(64);

    /// <summary>
    /// Perform a topological sort on the given list of sprites.
    /// </summary>
    public static List<IsoSpriteSorting> Sort(IReadOnlyList<IsoSpriteSorting> sprites) {
        var workingCopy = new List<IsoSpriteSorting>(sprites);

        // Keep breaking cycles until there are no more
        while (true) {
            circularDepStack.Clear();
            circularDepData.Clear();

            bool removedDependency = false;
            foreach (var sprite in workingCopy) {
                if (RemoveCircularDependencies(sprite)) removedDependency = true;
            }

            if (!removedDependency) break;
        }

        var sorted = new List<IsoSpriteSorting>(sprites.Count);
        visited.Clear();

        foreach (var sprite in sprites) {
            Visit(sprite, sorted, visited);
        }

        return sorted;
    }

    /// <summary>
    /// Recursively walks the graph, visiting each node in DFS fashion.
    /// </summary>
    private static void Visit(IsoSpriteSorting sprite, List<IsoSpriteSorting> sorted, HashSet<int> visited) {
        int id = sprite.GetInstanceID();

        if (!visited.Contains(id)) {
            visited.Add(id);

            foreach (var dependency in sprite.Dependencies) {
                Visit(dependency, sorted, visited);
            }

            sorted.Add(sprite);
        }
    }

    private static bool RemoveCircularDependencies(IsoSpriteSorting sprite) {
        circularDepStack.Add(sprite);
        bool removedDependency = false;

        int id = sprite.GetInstanceID();
        bool alreadyVisited = circularDepData.TryGetValue(id, out bool inProcess);
        if (alreadyVisited) {
            if (inProcess) {
                RemoveCircularDependencyFromStack();
                removedDependency = true;
            }
        } else {
            circularDepData[id] = true;
            for (int idx = 0; idx < sprite.Dependencies.Count; idx++) {
                if (RemoveCircularDependencies(sprite.Dependencies[idx])) removedDependency = true;
            }
            circularDepData[id] = false;
        }

        circularDepStack.RemoveAt(circularDepStack.Count - 1);
        return removedDependency;
    }

    private static void RemoveCircularDependencyFromStack() {
        if (circularDepStack.Count <= 1) return;

        var startingSorter = circularDepStack.Last();
        int repeatIndex = 0;
        for (int i = circularDepStack.Count - 2; i >= 0; i--) {
            IsoSpriteSorting sorter = circularDepStack[i];
            if (sorter == startingSorter) {
                repeatIndex = i;
                break;
            }
        }

        int weakestDepIndex = -1;
        float longestDistance = float.MinValue;

        FindWeakestDependency(
            repeatIndex,
            ref weakestDepIndex,
            ref longestDistance,
            (left, right) => left.sortType == IsoSortType.Point && right.sortType == IsoSortType.Point
        );

        if (weakestDepIndex == -1) {
            FindWeakestDependency(
                repeatIndex,
                ref weakestDepIndex,
                ref longestDistance
            );
        }

        var left = circularDepStack[weakestDepIndex];
        var right = circularDepStack[weakestDepIndex + 1];
        left.Dependencies.Remove(right);
    }

    /// <summary>
    /// Compares pairs of sprites in the stack and determines the weakest
    /// dependency as those furthest apart on the x-axis. This is essentially a
    /// weighting function on the topological sort that lets us break cycles.
    /// </summary>
    private static void FindWeakestDependency(
        int repeatIndex,
        ref int weakestDepIndex,
        ref float longestDistance,
        Func<IsoSpriteSorting, IsoSpriteSorting, bool> predicate = null
    ) {
        for (int i = repeatIndex; i < circularDepStack.Count - 1; i++) {
            var left = circularDepStack[i];
            var right = circularDepStack[i + 1];

            if (predicate?.Invoke(left, right) ?? true) {
                float dist = Mathf.Abs(left.AsPoint.x - right.AsPoint.x);
                if (dist > longestDistance) {
                    weakestDepIndex = i;
                    longestDistance = dist;
                }
            }
        }
    }
}
