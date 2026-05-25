using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
    public static MapNode GenerateTree(int layers = 8)
    {
        var root = new MapNode { id = 0, layer = 0, conditionType = NodeConditionType.None };
        int nextId = 1;
        var currentLayer = new List<MapNode> { root };

        for (int l = 0; l < layers - 1; l++)
        {
            var nextLayer = new List<MapNode>();
            foreach (var parent in currentLayer)
            {
                int count = l < 2 ? Random.Range(2, 4) : RollBranches();
                for (int b = 0; b < count; b++)
                {
                    var child = new MapNode
                    {
                        id = nextId++,
                        layer = l + 1,
                        indexInLayer = nextLayer.Count,
                        parent = parent,
                        conditionType = RollCondition()
                    };
                    parent.children.Add(child);
                    nextLayer.Add(child);
                }
            }
            currentLayer = nextLayer;
        }
        return root;
    }

    static int RollBranches()
    {
        float r = Random.value;
        return r < 0.15f ? 1 : r < 0.75f ? 2 : 3;
    }

    static NodeConditionType RollCondition()
    {
        float r = Random.value;
        return r < 0.30f ? NodeConditionType.None
             : r < 0.75f ? NodeConditionType.Normal
             : NodeConditionType.Hard;
    }
}
