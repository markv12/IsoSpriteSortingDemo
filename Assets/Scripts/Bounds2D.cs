using System;
using UnityEngine;

[Serializable]
public struct Bounds2D {
    public float minX;
    public float minY;
    public float maxX;
    public float maxY;

    public Bounds2D(Bounds bounds) {
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;
        minX = min.x;
        minY = min.y;
        maxX = max.x;
        maxY = max.y;
    }

    public Bounds2D(float _minX, float _minY, float _maxX, float _maxY) {
        minX = _minX;
        minY = _minY;
        maxX = _maxX;
        maxY = _maxY;
    }

    public bool Intersects(Bounds2D otherBounds) {
        return minX <= otherBounds.maxX && otherBounds.minX <= maxX && maxY >= otherBounds.minY && otherBounds.maxY >= minY;
    }

    public bool Contains(Vector2 point) {
        return minX <= point.x && maxX >= point.x && minY <= point.y && maxY >= point.y;
    }

    public Vector2 RandomPos() {
        float x = UnityEngine.Random.Range(minX, maxX);
        float y = UnityEngine.Random.Range(minY, maxY);
        return new Vector2(x, y);
    }

    public override string ToString() {
        return "Min: (" + minX + ", " + minY + ")  Max: (" + maxX + ", " + maxY + ")";
    }
}
