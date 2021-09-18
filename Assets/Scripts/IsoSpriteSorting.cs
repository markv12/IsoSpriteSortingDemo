using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsoSpriteSorting : MonoBehaviour
{
    public bool isMovable;
    public bool renderBelowAll;

    [NonSerialized]
    public bool registered = false;
    [NonSerialized]
    public bool forceSort;

    [NonSerialized]
    public readonly List<IsoSpriteSorting> staticDependencies = new List<IsoSpriteSorting>(16);
    [NonSerialized]
    public readonly List<IsoSpriteSorting> inverseStaticDependencies = new List<IsoSpriteSorting>(16);
    [NonSerialized]
    public readonly List<IsoSpriteSorting> movingDependencies = new List<IsoSpriteSorting>(8);

    private readonly List<IsoSpriteSorting> visibleStaticDependencies = new List<IsoSpriteSorting>(16);

    private int visibleStaticLastRefreshFrame = 0;
    public List<IsoSpriteSorting> VisibleStaticDependencies
    {
        get
        {
            if (visibleStaticLastRefreshFrame < Time.frameCount)
            {
                IsoSpriteSortingManager.FilterListByVisibility(staticDependencies, visibleStaticDependencies);
                visibleStaticLastRefreshFrame = Time.frameCount;
            }
            return visibleStaticDependencies;
        }
    }

    public List<IsoSpriteSorting> VisibleMovingDependencies
    {
        get
        {
            return movingDependencies;
        }
    }

    public enum SortType
    {
        Point,
        Line
    }

    public SortType sortType = SortType.Point;

    public Vector3 SorterPositionOffset = new Vector3();
    public Vector3 SorterPositionOffset2 = new Vector3();
    public Renderer[] renderersToSort;

    private Transform t;

    public void SetupStaticCache()
    {
        RefreshBounds();
        RefreshPoint1();
        RefreshPoint2();
    }

    private void RefreshBounds()
    {
        cachedBounds = new Bounds2D(renderersToSort[0].bounds);
    }

    private void RefreshPoint1()
    {
        cachedPoint1 = SorterPositionOffset + t.position;
    }

    private void RefreshPoint2()
    {
        cachedPoint2 = SorterPositionOffset2 + t.position;
    }

    private int lastPoint1CalculatedFrame;
    private Vector2 cachedPoint1;
    private Vector3 SortingPoint1
    {
        get
        {
            if (isMovable)
            {
                int frameCount = Time.frameCount;
                if (frameCount != lastPoint1CalculatedFrame)
                {
                    lastPoint1CalculatedFrame = frameCount;
                    RefreshPoint1();
                }
            }
            return cachedPoint1;
        }
    }

    private int lastPoint2CalculatedFrame;
    private Vector2 cachedPoint2;
    private Vector3 SortingPoint2
    {
        get
        {
            if (isMovable)
            {
                int frameCount = Time.frameCount;
                if (frameCount != lastPoint2CalculatedFrame)
                {
                    lastPoint2CalculatedFrame = frameCount;
                    RefreshPoint2();
                }
            }
            return cachedPoint2;
        }
    }

    public Vector3 AsPoint
    {
        get
        {
            if (sortType == SortType.Line)
                return ((SortingPoint1 + SortingPoint2) / 2);
            else
                return SortingPoint1;
        }
    }

    private float SortingLineCenterHeight
    {
        get
        {
            if (sortType == SortType.Line)
            {
                return ((SortingPoint1.y + SortingPoint2.y) / 2);
            }
            else
            {
                Debug.LogError("calling line center height on point type");
                return SortingPoint1.y;
            }
        }
    }

#if UNITY_EDITOR
    public void SortScene()
    {
        IsoSpriteSorting[] isoSorters = FindObjectsOfType(typeof(IsoSpriteSorting)) as IsoSpriteSorting[];
        for (int i = 0; i < isoSorters.Length; i++)
        {
            isoSorters[i].Setup();
        }
        IsoSpriteSortingManager.UpdateSorting();
        for (int i = 0; i < isoSorters.Length; i++)
        {
            isoSorters[i].Unregister();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
#endif

    private void Awake()
    {
        t = transform; //This needs to be here AND in the setup function
    }

    IEnumerator Start()
    {
        if (Application.isPlaying)
        {
            IsoSpriteSortingManager temp = IsoSpriteSortingManager.Instance; //bring the instance into existence so the Update function will run;
            yield return null;
            Setup();
        }
    }

    private void Setup()
    {
        t = transform;  //This needs to be here AND in the Awake function
        if (renderersToSort == null || renderersToSort.Length == 0)
        {
            renderersToSort = GetComponentsInChildren<Renderer>();
        }
        IsoSpriteSortingManager.RegisterSprite(this);
    }

    public static int CompairIsoSortersBasic(IsoSpriteSorting sprite1, IsoSpriteSorting sprite2)
    {
        float y1 = sprite1.sortType == SortType.Point ? sprite1.SortingPoint1.y : sprite1.SortingLineCenterHeight;
        float y2 = sprite2.sortType == SortType.Point ? sprite2.SortingPoint1.y : sprite2.SortingLineCenterHeight;
        return y2.CompareTo(y1);
    }

    //A result of -1 means sprite1 is above sprite2 in physical space
    public static int CompareIsoSorters(IsoSpriteSorting sprite1, IsoSpriteSorting sprite2)
    {
        if (sprite1.sortType == SortType.Point && sprite2.sortType == SortType.Point)
        {
            //Debug.Log(sprite1.name + " - " + sprite1.SortingPoint1 + " sprite2: " + sprite2.name + " - " + sprite2.SortingPoint1);
            return sprite2.SortingPoint1.y.CompareTo(sprite1.SortingPoint1.y);
        }
        else if (sprite1.sortType == SortType.Line && sprite2.sortType == SortType.Line)
        {
            return CompareLineAndLine(sprite1, sprite2);
        }
        else if (sprite1.sortType == SortType.Point && sprite2.sortType == SortType.Line)
        {
            return ComparePointAndLine(sprite1.SortingPoint1, sprite2);
        }
        else if (sprite1.sortType == SortType.Line && sprite2.sortType == SortType.Point)
        {
            return -ComparePointAndLine(sprite2.SortingPoint1, sprite1);
        }
        else
        {
            return 0;
        }
    }

    private static int CompareLineAndLine(IsoSpriteSorting line1, IsoSpriteSorting line2)
    {
        Vector2 line1Point1 = line1.SortingPoint1;
        Vector2 line1Point2 = line1.SortingPoint2;
        Vector2 line2Point1 = line2.SortingPoint1;
        Vector2 line2Point2 = line2.SortingPoint2;

        int comp1 = ComparePointAndLine(line1Point1, line2);
        int comp2 = ComparePointAndLine(line1Point2, line2);
        int oneVStwo = int.MinValue;
        if (comp1 == comp2) //Both points in line 1 are above or below line2
        {
            oneVStwo = comp1;
        }

        int comp3 = ComparePointAndLine(line2Point1, line1);
        int comp4 = ComparePointAndLine(line2Point2, line1);
        int twoVSone = int.MinValue;
        if (comp3 == comp4) //Both points in line 2 are above or below line1
        {
            twoVSone = -comp3;
        }

        if (oneVStwo != int.MinValue && twoVSone != int.MinValue)
        {
            if (oneVStwo == twoVSone) //the two comparisons agree about the ordering
            {
                return oneVStwo;
            }
            return CompareLineCenters(line1, line2);
        }
        else if (oneVStwo != int.MinValue)
        {
            return oneVStwo;
        }
        else if (twoVSone != int.MinValue)
        {
            return twoVSone;
        }

        else
        {
            return CompareLineCenters(line1, line2);
        }
    }

    private static int CompareLineCenters(IsoSpriteSorting line1, IsoSpriteSorting line2)
    {
        return -line1.SortingLineCenterHeight.CompareTo(line2.SortingLineCenterHeight);
    }

    private static int ComparePointAndLine(Vector3 point, IsoSpriteSorting line)
    {
        float pointY = point.y;
        if (pointY > line.SortingPoint1.y && pointY > line.SortingPoint2.y)
        {
            return -1;
        }
        else if (pointY < line.SortingPoint1.y && pointY < line.SortingPoint2.y)
        {
            return 1;
        }
        else
        {
            float slope = (line.SortingPoint2.y - line.SortingPoint1.y) / (line.SortingPoint2.x - line.SortingPoint1.x);
            float intercept = line.SortingPoint1.y - (slope * line.SortingPoint1.x);
            float yOnLineForPoint = (slope * point.x) + intercept;
            return yOnLineForPoint > point.y ? 1 : -1;
        }
    }

    private static bool PointWithinLineArea(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {
        bool xMatch = Mathf.Abs(linePoint1.x - point.x) + Mathf.Abs(linePoint2.x - point.x) == Mathf.Abs(linePoint1.x - linePoint2.x);
        bool yMatch = Mathf.Abs(linePoint1.y - point.y) + Mathf.Abs(linePoint2.y - point.y) == Mathf.Abs(linePoint1.y - linePoint2.y);
        return xMatch && yMatch;
    }

    public int RendererSortingOrder
    {
        get
        {
            if (renderersToSort.Length > 0)
            {
                return renderersToSort[0].sortingOrder;
            }
            else
            {
                return 0;
            }
        }
        set
        {
            for (int j = 0; j < renderersToSort.Length; ++j)
            {
                renderersToSort[j].sortingOrder = value;
            }
        }
    }

    private Bounds2D cachedBounds;
    private int lastBoundsCalculatedFrame = 0;
    public Bounds2D TheBounds
    {
        get
        {
            if (isMovable)
            {
                int frameCount = Time.frameCount;
                if (frameCount != lastBoundsCalculatedFrame)
                {
                    lastBoundsCalculatedFrame = frameCount;
                    RefreshBounds();
                }
            }
            return cachedBounds;
        }
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            Unregister();
        }
    }

    private void Unregister()
    {
        IsoSpriteSortingManager.UnregisterSprite(this);
    }
}
