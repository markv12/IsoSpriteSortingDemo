using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public enum IsoSortType {
    Point,
    Line
}

/// <summary>
/// Solves a variety of isometric sorting issues with a combination of point
/// and line sorting. Point sorting is similar to the traditional pivot point
/// sorting method. But for isometric games, it fails spectacularly in many
/// cases. For those cases, we rely on line sorting, that lets us declare a
/// range of sorting possibilities for each sprite. It's similar to the sorting
/// we'd achieve if the sprite were cut into tiles, but just done via maths.
///
/// For an explainer, see https://www.youtube.com/watch?v=yRZlVrinw9I.
/// Code courtesy of https://github.com/markv12/IsoSpriteSortingDemo.
/// </summary>
public class IsoSpriteSorting : MonoBehaviour, IComparable<IsoSpriteSorting>, ISerializationCallbackReceiver {
    public bool renderBelowAll;
    public IsoSortType sortType = IsoSortType.Point;
    public List<Vector3> sorterPositionOffsets;
    public Renderer[] renderersToSort;

    [NonSerialized]
    public bool registered = false;
    [NonSerialized]
    public bool forceSort;

    private List<Vector3> points = new List<Vector3>(4);
    private readonly List<IsoSpriteSorting> dependencies = new List<IsoSpriteSorting>(8);
    private Bounds2D bounds;

    [Inject]
    protected IsoSpriteSortingManager _manager;

    public List<Vector3> Points => points;
    public Bounds2D Bounds => bounds;

    public Vector3 AsPoint => sortType == IsoSortType.Line
        ? MedianVector(Points)
        : Points[0];

    public float SortingLineCenterHeight => sortType == IsoSortType.Line
        ? MedianY(Points)
        : throw new Exception("Sorting type is not line");

    public int SortingOrder {
        get {
            return renderersToSort.Length > 0
                ? renderersToSort[0].sortingOrder
                : 0;
        }
        set {
            foreach (var renderer in renderersToSort) {
                renderer.sortingOrder = value;
            }
        }
    }

    /// <summary>
    /// The list of sprites whose ordering influences this sprite's ordering.
    /// It's necessary for the topological sort to work. The sorting manager
    /// will update this list automatically.
    /// </summary>
    public List<IsoSpriteSorting> Dependencies => dependencies;

    protected Vector3 MedianVector(List<Vector3> vectors) => vectors.Aggregate(Vector3.zero, (acc, v) => acc + v) / vectors.Count;
    protected float MedianY(List<Vector3> vectors) => vectors.Sum(v => v.y) / vectors.Count;

    private void Start() {
        Setup();
    }

    protected void Update() {
        if (transform.hasChanged) {
            RefreshBounds();
            RefreshPoints();
            transform.hasChanged = false;
        }
    }

    private void OnDestroy() {
        Unregister();
    }

    public void Unregister() {
        _manager.UnregisterSprite(this);
    }

    public void Setup() {
        if (renderersToSort == null || renderersToSort.Length == 0) {
            renderersToSort = GetComponentsInChildren<Renderer>();
        }

        RefreshBounds();
        RefreshPoints();

        _manager.RegisterSprite(this);
    }

    public void OnBeforeSerialize() {
    }

    /// <summary>
    /// Add at least one point at the origin, which results in the same sorting
    /// as using the sprite pivot point.
    /// </summary>
    public void OnAfterDeserialize() {
        if (sorterPositionOffsets.Count == 0) {
            sorterPositionOffsets = new List<Vector3> { Vector3.zero };
        }
    }

    public int CompareTo(IsoSpriteSorting other) {
        return IsoSortComparisons.CompareIsoSorters(this, other);
    }

    private void RefreshBounds() {
        var groupBounds = renderersToSort[0].bounds;
        foreach (Renderer renderer in renderersToSort.Skip(1)) {
            groupBounds.Encapsulate(renderer.bounds);
        }
        bounds = new Bounds2D(groupBounds);
    }

    private void RefreshPoints() {
        points = sorterPositionOffsets
            .Select(p => p + transform.position)
            .ToList();
    }

    // Only for use in the editor
    public void InjectManager(IsoSpriteSortingManager manager) {
        _manager = manager;
    }
}
