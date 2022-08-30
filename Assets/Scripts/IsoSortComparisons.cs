using System.Linq;
using UnityEngine;

public static class IsoSortComparisons {
    /// <summary>
    /// Compares two IsoSprites based on their sort type.
    /// </summary>
    /// <returns>-1 if sprite1 is above sprite2, 1 if sprite2 is above sprite1</returns>
    public static int CompareIsoSorters(IsoSpriteSorting sprite1, IsoSpriteSorting sprite2) {
        if (sprite1.sortType == IsoSortType.Point && sprite2.sortType == IsoSortType.Point) {
            return sprite2.AsPoint.y.CompareTo(sprite1.AsPoint.y);
        } else if (sprite1.sortType == IsoSortType.Line && sprite2.sortType == IsoSortType.Line) {
            return CompareLineAndLine(sprite1, sprite2);
        } else if (sprite1.sortType == IsoSortType.Point && sprite2.sortType == IsoSortType.Line) {
            return ComparePointAndLine(sprite1.AsPoint, sprite2);
        } else if (sprite1.sortType == IsoSortType.Line && sprite2.sortType == IsoSortType.Point) {
            return -ComparePointAndLine(sprite2.AsPoint, sprite1);
        } else {
            return 0;
        }
    }

    private static int CompareLineAndLine(IsoSpriteSorting line1, IsoSpriteSorting line2) {
        Vector2 line1Start = line1.Points[0];
        Vector2 line1End = line1.Points[line1.Points.Count - 1];
        Vector2 line2Start = line2.Points[0];
        Vector2 line2End = line2.Points[line2.Points.Count - 1];

        int comp1 = ComparePointAndLine(line1Start, line2);
        int comp2 = ComparePointAndLine(line1End, line2);
        int oneVsTwo = int.MinValue;

        // Line1 is above or below line2
        if (comp1 == comp2) {
            oneVsTwo = comp1;
        }

        int comp3 = ComparePointAndLine(line2Start, line1);
        int comp4 = ComparePointAndLine(line2End, line1);
        int twoVsOne = int.MinValue;

        // Line2 is above or below line1
        if (comp3 == comp4) {
            twoVsOne = -comp3;
        }

        if (oneVsTwo != int.MinValue && twoVsOne != int.MinValue) {
            // The two comparisons agree about the ordering
            if (oneVsTwo == twoVsOne) {
                return oneVsTwo;
            }
            return CompareLineCenters(line1, line2);
        } else if (oneVsTwo != int.MinValue) {
            return oneVsTwo;
        } else if (twoVsOne != int.MinValue) {
            return twoVsOne;
        } else {
            return CompareLineCenters(line1, line2);
        }
    }

    private static int CompareLineCenters(IsoSpriteSorting line1, IsoSpriteSorting line2) {
        return -line1.SortingLineCenterHeight.CompareTo(line2.SortingLineCenterHeight);
    }

    private static int ComparePointAndLine(Vector3 point, IsoSpriteSorting line) {
        if (line.Points.Count > 2) {
            return ComparePointWithLineSegments(point, line);
        } else {
            return ComparePointWithLineSegment(point, line);
        }
    }

    private static int ComparePointWithLineSegments(Vector3 point, IsoSpriteSorting line) {
        // Build a set of line segments using pairwise iteration
        var segments = line.Points.Zip(line.Points.Skip(1), (start, end) => (start, end));

        // Find the segment that the point is closest to
        var closestPoint = segments
            .Select(seg => GetClosestPointOnLineSegment(point, seg))
            .FirstOrDefault(p => p != null);

        if (!closestPoint.HasValue) {
            return ComparePointWithLineSegment(point, line);
        }
        return closestPoint.Value.y > point.y ? 1 : -1;
    }

    private static int ComparePointWithLineSegment(Vector3 point, IsoSpriteSorting line) {
        var lineStart = line.Points[0];
        var lineEnd = line.Points[line.Points.Count - 1];

        // Simple cases: the point is above or below the entire line segment
        if (point.y > lineStart.y && point.y > lineEnd.y) {
            return -1;
        } else if (point.y < lineStart.y && point.y < lineEnd.y) {
            return 1;
        }

        // The point is between the start and end points, use the projection
        float slope = (lineEnd.y - lineStart.y) / (lineEnd.x - lineStart.x);
        float intercept = lineStart.y - (slope * lineStart.x);
        float yOnLineForPoint = (slope * point.x) + intercept;
        return yOnLineForPoint > point.y ? 1 : -1;
    }

    private static Vector3? GetClosestPointOnLineSegment(Vector3 point, (Vector3 a, Vector3 b) seg) {
        Vector3 AP = point - seg.a;
        Vector3 AB = seg.b - seg.a;

        // The normalized "distance" from a to your closest point
        float distance = Vector2.Dot(AP, AB) / AB.sqrMagnitude;

        // Check if P projection is over vector AB
        if (distance < 0 || distance > 1) {
            return null;
        } else {
            return seg.a + AB * distance;
        }
    }
}
