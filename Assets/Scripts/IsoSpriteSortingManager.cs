using System.Collections.Generic;
using UnityEngine;

public class IsoSpriteSortingManager : Singleton<IsoSpriteSortingManager> {
    private static readonly List<IsoSpriteSorting> floorSpriteList = new List<IsoSpriteSorting>(256);
    private static readonly List<IsoSpriteSorting> staticSpriteList = new List<IsoSpriteSorting>(8192);
    private static readonly List<IsoSpriteSorting> currentlyVisibleStaticSpriteList = new List<IsoSpriteSorting>(256);

    private static readonly List<IsoSpriteSorting> moveableSpriteList = new List<IsoSpriteSorting>(256);
    private static readonly List<IsoSpriteSorting> currentlyVisibleMoveableSpriteList = new List<IsoSpriteSorting>(256);

    public static void RegisterSprite(IsoSpriteSorting newSprite) {
        if (!newSprite.registered) {
            if (newSprite.RenderBelowAll) {
                floorSpriteList.Add(newSprite);
                SortListSimple(floorSpriteList);
                SetSortOrderNegative(floorSpriteList);
            } else {
                if (newSprite.isMovable) {
                    moveableSpriteList.Add(newSprite);
                } else {
                    staticSpriteList.Add(newSprite);
                    SetupStaticDependencies(newSprite);
                }
            }
            newSprite.registered = true;
        }
    }

    private static void SetupStaticDependencies(IsoSpriteSorting newSprite) {
        int theCount = staticSpriteList.Count;
        for (int i = 0; i < theCount; i++) {
            IsoSpriteSorting otherSprite = staticSpriteList[i];
            Bounds2D b1 = newSprite.cachedBounds;
            Bounds2D b2 = otherSprite.cachedBounds;
            if (b1.minX <= b2.maxX && b2.minX <= b1.maxX && b1.maxY >= b2.minY && b2.maxY >= b1.minY) { //Inline Bounds2D.Intersects
                int compareResult = IsoSpriteSorting.CompareIsoSorters(newSprite, otherSprite);
                //Debug.Log("Compared: " + newSprite.gameObject.name + " other: " + otherSprite.gameObject.name + " result: " + compareResult);
                if (compareResult == -1) {
                    otherSprite.staticDependencies.Add(newSprite);
                    newSprite.inverseStaticDependencies.Add(otherSprite);
                } else if (compareResult == 1) {
                    newSprite.staticDependencies.Add(otherSprite);
                    otherSprite.inverseStaticDependencies.Add(newSprite);
                }
            }
        }
    }

    public static void UnregisterSprite(IsoSpriteSorting spriteToRemove) {
        if (spriteToRemove.registered) {
            if (spriteToRemove.RenderBelowAll) {
                floorSpriteList.Remove(spriteToRemove);
            } else {
                if (spriteToRemove.isMovable) {
                    moveableSpriteList.Remove(spriteToRemove);
                } else {
                    staticSpriteList.Remove(spriteToRemove);
                    RemoveStaticDependencies(spriteToRemove);
                }
            }
            spriteToRemove.registered = false;
        }
    }

    private static void RemoveStaticDependencies(IsoSpriteSorting spriteToRemove) {
        for (int i = 0; i < spriteToRemove.inverseStaticDependencies.Count; i++) {
            IsoSpriteSorting otherSprite = spriteToRemove.inverseStaticDependencies[i];
            otherSprite.staticDependencies.Remove(spriteToRemove);
        }
        spriteToRemove.inverseStaticDependencies.Clear();
        spriteToRemove.staticDependencies.Clear();
    }

    private void Update() {
        UpdateSorting();
    }

    private void LateUpdate() {
        for (int i = 0; i < moveableSpriteList.Count; i++) {
            moveableSpriteList[i].LateUpdateHasChanged();
        }
    }

    private static readonly List<IsoSpriteSorting> sortedSprites = new List<IsoSpriteSorting>(64);
    public static void UpdateSorting() {
        ChechCacheRefreshes(moveableSpriteList);
        FilterListByVisibility(staticSpriteList, currentlyVisibleStaticSpriteList);
        FilterListByVisibility(moveableSpriteList, currentlyVisibleMoveableSpriteList);

        ClearMovingDependencies(currentlyVisibleStaticSpriteList);
        ClearMovingDependencies(currentlyVisibleMoveableSpriteList);

        AddMovingDependencies(currentlyVisibleMoveableSpriteList, currentlyVisibleStaticSpriteList);

        sortedSprites.Clear();
        TopologicalSort.Sort(currentlyVisibleStaticSpriteList, currentlyVisibleMoveableSpriteList, sortedSprites);
        SetSortOrderBasedOnListOrder(sortedSprites);
    }

    private static void ChechCacheRefreshes(List<IsoSpriteSorting> sorters) {
        for (int i = 0; i < sorters.Count; i++) {
            sorters[i].CheckCacheRefresh();
        }
    }

    private static void AddMovingDependencies(List<IsoSpriteSorting> moveableList, List<IsoSpriteSorting> staticList) {
        int moveableCount = moveableList.Count;
        int staticCount = staticList.Count;
        for (int i = 0; i < moveableCount; i++) {
            IsoSpriteSorting moveSprite1 = moveableList[i];
            //Add Moving Dependencies to static sprites
            for (int j = 0; j < staticCount; j++) {
                IsoSpriteSorting staticSprite = staticList[j];
                if (CalculateBoundsIntersection(moveSprite1, staticSprite)) {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(moveSprite1, staticSprite);
                    if (compareResult == -1) {
                        staticSprite.movingDependencies.Add(moveSprite1);
                    } else if (compareResult == 1) {
                        moveSprite1.movingDependencies.Add(staticSprite);
                    }
                }
            }
            //Add Moving Dependencies to Moving Sprites
            for (int j = 0; j < moveableCount; j++) {
                IsoSpriteSorting moveSprite2 = moveableList[j];
                if (CalculateBoundsIntersection(moveSprite1, moveSprite2)) {
                    int compareResult = IsoSpriteSorting.CompareIsoSorters(moveSprite1, moveSprite2);
                    if (compareResult == -1) {
                        moveSprite2.movingDependencies.Add(moveSprite1);
                    }
                }
            }
        }
    }

    private static void ClearMovingDependencies(List<IsoSpriteSorting> sprites) {
        int count = sprites.Count;
        for (int i = 0; i < count; i++) {
            sprites[i].movingDependencies.Clear();
        }
    }

    private static bool CalculateBoundsIntersection(IsoSpriteSorting sprite, IsoSpriteSorting otherSprite) {
        return sprite.cachedBounds.Intersects(otherSprite.cachedBounds);
    }

    private static void SetSortOrderBasedOnListOrder(List<IsoSpriteSorting> spriteList) {
        int orderCurrent = 0;
        int count = spriteList.Count;
        for (int i = 0; i < count; ++i) {
            spriteList[i].RendererSortingOrder = orderCurrent;
            orderCurrent += 2;
        }
    }

    private static void SetSortOrderNegative(List<IsoSpriteSorting> spriteList) {
        int order = (-spriteList.Count - 1) * 2;
        for (int i = 0; i < spriteList.Count; i++) {
            spriteList[i].RendererSortingOrder = order;
            order += 2;
        }
    }

    private const float SORT_RANGE = 80f;
    public static void FilterListByVisibility(List<IsoSpriteSorting> fullList, List<IsoSpriteSorting> destinationList) {
        destinationList.Clear();
        if (Camera.main != null) {
            Vector2 cameraPos = Camera.main.transform.position;
            float cameraPosX = cameraPos.x;
            float cameraPosY = cameraPos.y;
            int count = fullList.Count;
            for (int i = 0; i < count; i++) {
                IsoSpriteSorting sprite = fullList[i];
                if (sprite.forceSort) {
                    destinationList.Add(sprite);
                    sprite.forceSort = false;
                } else {
                    float diffX = sprite.SortingPoint1.x - cameraPosX;
                    float diffY = sprite.SortingPoint1.y - cameraPosY;
                    if (diffX < SORT_RANGE && diffX > -SORT_RANGE && diffY < SORT_RANGE && diffX > -SORT_RANGE) {
                        for (int j = 0; j < sprite.renderersToSort.Length; j++) {
                            //if (sprite.renderersToSort[j] == null)
                            //{
                            //    UnityEngine.Debug.Log(sprite.gameObject.name);
                            //}
                            if (sprite.renderersToSort[j].isVisible) {
                                destinationList.Add(sprite);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private static void SortListSimple(List<IsoSpriteSorting> list) {
        list.Sort((a, b) => {
            if (!a || !b) {
                return 0;
            } else {
                return IsoSpriteSorting.CompareIsoSorters(a, b);
            }
        });
    }
}
