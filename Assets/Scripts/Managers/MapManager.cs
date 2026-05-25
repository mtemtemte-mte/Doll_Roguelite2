using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public MapNode Root { get; private set; }
    public MapNode CurrentNode { get; private set; }

    public System.Action OnMapChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Root = MapGenerator.GenerateTree(8);
        CurrentNode = Root;
        UpdateVisibility();
    }

    public bool TryMoveToNode(MapNode node)
    {
        if (!CurrentNode.children.Contains(node)) return false;
        CurrentNode.isCleared = true;
        CurrentNode.state = NodeState.Cleared;
        CurrentNode = node;
        UpdateVisibility();
        OnMapChanged?.Invoke();
        return true;
    }

    void UpdateVisibility()
    {
        CurrentNode.state = NodeState.Current;

        foreach (var child in CurrentNode.children)
        {
            if (child.state != NodeState.Cleared)
                child.state = NodeState.Visible;

            foreach (var grand in child.children)
                if (grand.state != NodeState.Cleared)
                    grand.state = NodeState.RouteOnly;
        }
    }
}
