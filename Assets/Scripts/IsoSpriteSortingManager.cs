using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the list of IsoSpriteSorting objects, assigns dependencies based on
/// bounds checks, and performs a topological sort to determine their rendering
/// order.
/// </summary>
public class IsoSpriteSortingManager : MonoBehaviour {
    private readonly List<IsoSpriteSorting> backgroundSpriteList = new List<IsoSpriteSorting>(64);
    private readonly List<IsoSpriteSorting> actorSpriteList = new List<IsoSpriteSorting>(64);

    private List<IsoSpriteSorting> visibleActorSpriteList;

    protected void Update() {
        UpdateSorting();
    }

    /// <summary>
    /// Update the sort order with the following steps:
    ///
    /// 1. Filter the list of all sprites to only include those that are visible
    /// 2. Populate dependency lists for each sprite based on their bounds
    /// 3. Perform a topological sort on the sprites to determine their order
    /// </summary>
    public void UpdateSorting() {
        visibleActorSpriteList = FilterByVisibility(actorSpriteList);
        visibleActorSpriteList.ForEach(s => s.Dependencies.Clear());
        AddDependencies(visibleActorSpriteList);

        var sortedSprites = TopologicalSort.Sort(visibleActorSpriteList);
        SetSortOrderBasedOnListOrder(sortedSprites);
    }

    public void RegisterSprite(IsoSpriteSorting newSprite) {
        if (!newSprite.registered) {
            if (newSprite.renderBelowAll) {
                backgroundSpriteList.Add(newSprite);
                backgroundSpriteList.Sort();
                SetSortOrderNegative(backgroundSpriteList);
            } else {
                actorSpriteList.Add(newSprite);
            }
            newSprite.registered = true;
        }
    }

    public void UnregisterSprite(IsoSpriteSorting spriteToRemove) {
        if (spriteToRemove.registered) {
            if (spriteToRemove.renderBelowAll) {
                backgroundSpriteList.Remove(spriteToRemove);
            } else {
                actorSpriteList.Remove(spriteToRemove);
            }
            spriteToRemove.registered = false;
        }
    }

    /// <summary>
    /// A dependency between two sprites exists when they have overlapping
    /// bounds. We compare two overlapping sprites and add a dependency based
    /// on their sort order. This allows us to perform a topological sort.
    /// </summary>
    private void AddDependencies(List<IsoSpriteSorting> sprites) {
        var pairs = sprites.UniqueCombinations(2);
        foreach (var pair in pairs) {
            var left = pair.First();
            var right = pair.Last();

            if (left.Bounds.Intersects(right.Bounds)) {
                int compareResult = IsoSortComparisons.CompareIsoSorters(left, right);
                if (compareResult == 1) {
                    left.Dependencies.Add(right);
                } else if (compareResult == -1) {
                    right.Dependencies.Add(left);
                }
            }
        }
    }

    private void SetSortOrderBasedOnListOrder(List<IsoSpriteSorting> spriteList) {
        for (int order = 0; order < spriteList.Count; order++) {
            spriteList[order].SortingOrder = order;
        }
    }

    /// <summary>
    /// Start at a negative index and count forward to zero. This is for
    /// background sprites (aka, renderBelowAll).
    /// </summary>
    private void SetSortOrderNegative(List<IsoSpriteSorting> spriteList) {
        int startOrder = -spriteList.Count - 1;
        for (int i = 0; i < spriteList.Count; ++i) {
            spriteList[i].SortingOrder = startOrder + i;
        }
    }

    public List<IsoSpriteSorting> FilterByVisibility(List<IsoSpriteSorting> fullList) {
        var forcedSort = fullList.Where(s => s.forceSort).ToList();
        var visible = fullList.Where(s => s.renderersToSort.Any(r => r.isVisible)).ToList();
        forcedSort.ForEach(s => s.forceSort = false);
        return forcedSort.Concat(visible).ToList();
    }
}

public static class Combinations {
    public static IEnumerable<IEnumerable<T>> UniqueCombinations<T>(this IEnumerable<T> elements, int k) {
        return k == 0
            ? Enumerable.Repeat(Enumerable.Empty<T>(), 1)
            : elements.SelectMany((e, i) =>
                elements.Skip(i + 1)
                    .UniqueCombinations(k - 1)
                    .Select(c => Enumerable.Repeat(e, 1).Concat(c))
            );
    }
}
