using System.Collections.Generic;

public class IsoSpriteSortingManager : Singleton<IsoSpriteSortingManager>
{
    private static readonly List<IsoSpriteSorting> floorSpriteList = new List<IsoSpriteSorting>(64);
    private static readonly List<IsoSpriteSorting> staticSpriteList = new List<IsoSpriteSorting>(256);
    private static readonly List<IsoSpriteSorting> currentlyVisibleStaticSpriteList = new List<IsoSpriteSorting>();

    private static readonly List<IsoSpriteSorting> moveableSpriteList = new List<IsoSpriteSorting>(64);
    private static readonly List<IsoSpriteSorting> currentlyVisibleMoveableSpriteList = new List<IsoSpriteSorting>();

    public static void RegisterSprite(IsoSpriteSorting newSprite)
    {
        if (!newSprite.registered)
        {
            if (newSprite.renderBelowAll)
            {
                floorSpriteList.Add(newSprite);
                SortListSimple(floorSpriteList);
                SetSortOrderNegative(floorSpriteList);
            }
            else
            {
                if (newSprite.isMovable)
                {
                    moveableSpriteList.Add(newSprite);
                }
                else
                {

                    staticSpriteList.Add(newSprite);
                    newSprite.SetupStaticCache();
                    SetupStaticDependencies(newSprite);
                }
            }
            newSprite.registered = true;
        }
    }

    private static void SetupStaticDependencies(IsoSpriteSorting newSprite)
    {
        int the_count = staticSpriteList.Count;
        for (int i = 0; i < the_count; i++)
        {
            IsoSpriteSorting otherSprite = staticSpriteList[i];
            if (CalculateBoundsIntersection(newSprite, otherSprite))
            {
                int compareResult = IsoSpriteSorting.CompareIsoSorters(newSprite, otherSprite);
                //Debug.Log("Compared: " + newSprite.gameObject.name + " other: " + otherSprite.gameObject.name + " result: " + compareResult);
                if (compareResult == -1)
                {
                    otherSprite.staticDependencies.Add(newSprite);
                    newSprite.inverseStaticDependencies.Add(otherSprite);
                }
                else if (compareResult == 1)
                {
                    newSprite.staticDependencies.Add(otherSprite);
                    otherSprite.inverseStaticDependencies.Add(newSprite);
                }
            }
        }
    }

    public static void UnregisterSprite(IsoSpriteSorting spriteToRemove)
    {
        if (spriteToRemove.registered)
        {
            if (spriteToRemove.renderBelowAll)
            {
                floorSpriteList.Remove(spriteToRemove);
            }
            else
            {
                if (spriteToRemove.isMovable)
                {
                    moveableSpriteList.Remove(spriteToRemove);
                }
                else
                {
                    staticSpriteList.Remove(spriteToRemove);
                    RemoveStaticDependencies(spriteToRemove);
                }
            }
            spriteToRemove.registered = false;
        }
    }

    private static void RemoveStaticDependencies(IsoSpriteSorting spriteToRemove)
    {
        for (int i = 0; i < spriteToRemove.inverseStaticDependencies.Count; i++)
        {
            IsoSpriteSorting otherSprite = spriteToRemove.inverseStaticDependencies[i];
            otherSprite.staticDependencies.Remove(spriteToRemove);
        }
        spriteToRemove.inverseStaticDependencies.Clear();
        spriteToRemove.staticDependencies.Clear();
    }

    void Update()
    {
        UpdateSorting();
    }

    private static readonly List<IsoSpriteSorting> sortedSprites = new List<IsoSpriteSorting>(64);
    public static void UpdateSorting()
    {
        FilterListByVisibility(staticSpriteList, currentlyVisibleStaticSpriteList);
        FilterListByVisibility(moveableSpriteList, currentlyVisibleMoveableSpriteList);

        ClearMovingDependencies(currentlyVisibleStaticSpriteList);
        ClearMovingDependencies(currentlyVisibleMoveableSpriteList);

        AddMovingDependencies(currentlyVisibleMoveableSpriteList, currentlyVisibleStaticSpriteList);

        sortedSprites.Clear();
        TopologicalSort.Sort(currentlyVisibleStaticSpriteList, currentlyVisibleMoveableSpriteList, sortedSprites);
        SetSortOrderBasedOnListOrder(sortedSprites);
    }

    private static void AddMovingDependencies(List<IsoSpriteSorting> moveableList, List<IsoSpriteSorting> staticList)
    {
        int moveableCount = moveableList.Count;
        int staticCount = staticList.Count;
        for (int i = 0; i < moveableCount; i++)
        {
            IsoSpriteSorting moveSprite1 = moveableList[i];
            //Add Moving Dependencies to static sprites
            for (int j = 0; j < staticCount; j++)
            {
                IsoSpriteSorting staticSprite = staticList[j];
                if (CalculateBoundsIntersection(moveSprite1, staticSprite))
                {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(moveSprite1, staticSprite);
                    if (compareResult == -1)
                    {
                        staticSprite.movingDependencies.Add(moveSprite1);
                    }
                    else if (compareResult == 1)
                    {
                        moveSprite1.movingDependencies.Add(staticSprite);
                    }
                }
            }
            //Add Moving Dependencies to Moving Sprites
            for (int j = 0; j < moveableCount; j++)
            {
                IsoSpriteSorting moveSprite2 = moveableList[j];
                if (CalculateBoundsIntersection(moveSprite1, moveSprite2))
                {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(moveSprite1, moveSprite2);
                    if (compareResult == -1)
                    {
                        moveSprite2.movingDependencies.Add(moveSprite1);
                    }
                }
            }
        }
    }

    private static void ClearMovingDependencies(List<IsoSpriteSorting> sprites)
    {
        int count = sprites.Count;
        for (int i = 0; i < count; i++)
        {
            sprites[i].movingDependencies.Clear();
        }
    }

    private static bool CalculateBoundsIntersection(IsoSpriteSorting sprite, IsoSpriteSorting otherSprite)
    {
        return sprite.TheBounds.Intersects(otherSprite.TheBounds);
    }

    private static void SetSortOrderBasedOnListOrder(List<IsoSpriteSorting> spriteList)
    {
        int orderCurrent = 0;
        int count = spriteList.Count;
        for (int i = 0; i < count; ++i)
        {
            spriteList[i].RendererSortingOrder = orderCurrent;
            orderCurrent += 1;
        }
    }

    private static void SetSortOrderNegative(List<IsoSpriteSorting> spriteList)
    {
        int startOrder = -spriteList.Count - 1;
        for (int i = 0; i < spriteList.Count; ++i)
        {
            spriteList[i].RendererSortingOrder = startOrder + i;
        }
    }

    public static void FilterListByVisibility(List<IsoSpriteSorting> fullList, List<IsoSpriteSorting> destinationList)
    {
        destinationList.Clear();
        int count = fullList.Count;
        for (int i = 0; i < count; i++)
        {
            IsoSpriteSorting sprite = fullList[i];
            if (sprite.forceSort)
            {
                destinationList.Add(sprite);
                sprite.forceSort = false;
            }
            else
            {
                for (int j = 0; j < sprite.renderersToSort.Length; j++)
                {
                    if (sprite.renderersToSort[j].isVisible)
                    {
                        destinationList.Add(sprite);
                        break;
                    }
                }
            }
        }
    }

    private static void SortListSimple(List<IsoSpriteSorting> list)
    {
        list.Sort((a, b) =>
        {
            if (!a || !b)
            {
                return 0;
            }
            else
            {
                return IsoSpriteSorting.CompareIsoSorters(a, b);
            }
        });
    }
}
