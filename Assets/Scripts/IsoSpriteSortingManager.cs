using System.Collections.Generic;

public class IsoSpriteSortingManager : Singleton<IsoSpriteSortingManager>
{
    private static List<IsoSpriteSorting> floorSpriteList = new List<IsoSpriteSorting>(64);
    private static List<IsoSpriteSorting> staticSpriteList = new List<IsoSpriteSorting>(256);
    private static List<IsoSpriteSorting> currentlyVisibleStaticSpriteList = new List<IsoSpriteSorting>();

    private static List<IsoSpriteSorting> moveableSpriteList = new List<IsoSpriteSorting>(64);
    private static List<IsoSpriteSorting> currentlyVisibleMoveableSpriteList = new List<IsoSpriteSorting>();

    public static void RegisterSprite(IsoSpriteSorting newSprite)
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
                SetupStaticDependencies(newSprite);
            }
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
    }

    private static void RemoveStaticDependencies(IsoSpriteSorting spriteToRemove)
    {
        for (int i = 0; i < spriteToRemove.inverseStaticDependencies.Count; i++)
        {
            IsoSpriteSorting otherSprite = spriteToRemove.inverseStaticDependencies[i];
            otherSprite.staticDependencies.Remove(spriteToRemove);
        }
    }

    void Update()
    {
        UpdateSorting();
    }

    private static List<IsoSpriteSorting> sortedSprites = new List<IsoSpriteSorting>(64);
    public static void UpdateSorting()
    {
        FilterListByVisibility(staticSpriteList, currentlyVisibleStaticSpriteList);
        FilterListByVisibility(moveableSpriteList, currentlyVisibleMoveableSpriteList);

        ClearMovingDependencies(currentlyVisibleStaticSpriteList);
        ClearMovingDependencies(currentlyVisibleMoveableSpriteList);

        AddMovingDependenciesToStaticSprites(currentlyVisibleMoveableSpriteList, currentlyVisibleStaticSpriteList);
        AddMovingDependenciesToMovingSprites(currentlyVisibleMoveableSpriteList);

        sortedSprites.Clear();
        TopologicalSort.Sort(currentlyVisibleStaticSpriteList, currentlyVisibleMoveableSpriteList, sortedSprites);
        SetSortOrderBasedOnListOrder(sortedSprites);
    }

    private static void AddMovingDependenciesToStaticSprites(List<IsoSpriteSorting> moveableList, List<IsoSpriteSorting> staticList)
    {
        for (int i = 0; i < moveableList.Count; i++)
        {
            IsoSpriteSorting moveSprite = moveableList[i];
            for (int j = 0; j < staticList.Count; j++)
            {
                IsoSpriteSorting staticSprite = staticList[j];
                if (CalculateBoundsIntersection(moveSprite, staticSprite))
                {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(moveSprite, staticSprite);
                    if (compareResult == -1)
                    {
                        staticSprite.movingDependencies.Add(moveSprite);
                    }
                    else if (compareResult == 1)
                    {
                        moveSprite.movingDependencies.Add(staticSprite);
                    }
                }
            }
        }
    }

    private static void AddMovingDependenciesToMovingSprites(List<IsoSpriteSorting> moveableList)
    {
        for (int i = 0; i < moveableList.Count; i++)
        {
            IsoSpriteSorting sprite1 = moveableList[i];
            for (int j = 0; j < moveableList.Count; j++)
            {
                IsoSpriteSorting sprite2 = moveableList[j];
                if (CalculateBoundsIntersection(sprite1, sprite2))
                {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(sprite1, sprite2);
                    if (compareResult == -1)
                    {
                        sprite2.movingDependencies.Add(sprite1);
                    }
                }
            }
        }
    }

    private static void ClearMovingDependencies(List<IsoSpriteSorting> sprites)
    {
        for (int i = 0; i < sprites.Count; i++)
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
        for (int i = 0; i < spriteList.Count; ++i)
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
        for (int i = 0; i < fullList.Count; i++)
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
