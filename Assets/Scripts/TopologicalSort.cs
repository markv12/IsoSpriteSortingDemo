using System.Collections.Generic;
public static class TopologicalSort
{
    public static Dictionary<IsoSpriteSorting, bool> visited = new Dictionary<IsoSpriteSorting, bool>(64);
    public static List<IsoSpriteSorting> allSprites = new List<IsoSpriteSorting>(64);
    public static List<IsoSpriteSorting> Sort(List<IsoSpriteSorting> staticSprites, List<IsoSpriteSorting> movableSprites, List<IsoSpriteSorting> sorted)
    {
        allSprites.Clear();
        allSprites.AddRange(staticSprites);
        allSprites.AddRange(movableSprites);
        visited.Clear();
        for (int i = 0; i < allSprites.Count; i++)
        {
            Visit(allSprites[i], sorted, visited);
        }

        return sorted;
    }

    public static void Visit(IsoSpriteSorting item, List<IsoSpriteSorting> sorted, Dictionary<IsoSpriteSorting, bool> visited)
    {
        bool inProcess;
        var alreadyVisited = visited.TryGetValue(item, out inProcess);

        if (alreadyVisited)
        {
            //if (inProcess)
            //{
            //    string result = "";
            //    for (int i = 0; i < item.dependencies.Count; i++)
            //    {
            //        result += item.dependencies[i].name + System.Environment.NewLine;
            //    }
            //    UnityEngine.Debug.Log("Cyclic dependency found: " + item.name);
            //    UnityEngine.Debug.Log(result);
            //}
        }
        else
        {
            visited[item] = true;

            List<IsoSpriteSorting> dependencies = item.ActiveDependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                Visit(dependencies[i], sorted, visited);
            }

            visited[item] = false;
            sorted.Add(item);
        }
    }
}
