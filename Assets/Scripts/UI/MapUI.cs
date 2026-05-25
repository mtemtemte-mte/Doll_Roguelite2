using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapUI : MonoBehaviour
{
    [SerializeField] float layerSpacing = 2f;
    [SerializeField] float mapWidth = 14f;
    [SerializeField] float nodeRadius = 0.35f;

    static readonly Color ColCurrent    = new(1.00f, 0.65f, 0.10f, 1f); // 주황
    static readonly Color ColCleared    = new(0.15f, 0.15f, 0.15f, 1f); // 검정
    static readonly Color ColNone       = new(0.15f, 0.15f, 0.15f, 1f); // 검정 (조건없음)
    static readonly Color ColNormal     = new(0.25f, 0.50f, 1.00f, 1f); // 파랑
    static readonly Color ColHard       = new(0.65f, 0.10f, 0.15f, 1f); // 진빨강
    static readonly Color ColRouteOnly  = new(0.55f, 0.55f, 0.55f, 1f); // 회색
    static readonly Color ColLine       = new(0.40f, 0.40f, 0.40f, 1f);

    Sprite circleSprite;
    readonly Dictionary<MapNode, SpriteRenderer> nodeRenderers = new();
    readonly Dictionary<MapNode, CircleCollider2D> nodeColliders = new();
    readonly List<(LineRenderer lr, MapNode from, MapNode to)> lines = new();

    void Start()
    {
        circleSprite = MakeCircleSprite(64);
        MapManager.Instance.OnMapChanged += Refresh;
        Build(MapManager.Instance.Root);
        Refresh();
    }

    void Build(MapNode root)
    {
        var layers = CollectLayers(root);

        // 위치 계산
        for (int l = 0; l < layers.Count; l++)
        {
            var layer = layers[l];
            for (int i = 0; i < layer.Count; i++)
            {
                float x = layer.Count == 1 ? 0f
                    : (i / (float)(layer.Count - 1) - 0.5f) * mapWidth;
                float y = -l * layerSpacing;
                layer[i].position = new Vector2(x, y);
                CreateNodeGO(layer[i]);
            }
        }

        // 연결선
        foreach (var node in nodeRenderers.Keys)
            foreach (var child in node.children)
                CreateLine(node, child);

        // 카메라 위치 조정
        float h = (layers.Count - 1) * layerSpacing;
        Camera.main.transform.position = new Vector3(0, -h / 2f, -10f);
        Camera.main.orthographicSize = Mathf.Max(h / 2f + 2f, mapWidth / Camera.main.aspect / 2f + 1f);
    }

    void CreateNodeGO(MapNode node)
    {
        var go = new GameObject($"Node_{node.id}");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(node.position.x, node.position.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingOrder = 2;
        go.transform.localScale = Vector3.one * nodeRadius * 2f;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        nodeRenderers[node] = sr;
        nodeColliders[node] = col;
    }

    void CreateLine(MapNode from, MapNode to)
    {
        var go = new GameObject($"Line_{from.id}_{to.id}");
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(from.position.x, from.position.y, 0.1f));
        lr.SetPosition(1, new Vector3(to.position.x, to.position.y, 0.1f));
        lr.startWidth = lr.endWidth = 0.05f;
        lr.useWorldSpace = true;
        lr.sortingOrder = 1;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", ColLine);
        lr.material = mat;

        lines.Add((lr, from, to));
    }

    void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        var worldPos = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        var hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        foreach (var (node, col) in nodeColliders)
            if (col == hit && MapManager.Instance.TryMoveToNode(node))
                break;
    }

    void Refresh()
    {
        foreach (var (node, sr) in nodeRenderers)
        {
            bool hidden = node.state == NodeState.Hidden;
            sr.gameObject.SetActive(!hidden);
            if (!hidden) sr.color = GetColor(node);
        }

        foreach (var (lr, from, to) in lines)
            lr.gameObject.SetActive(from.state != NodeState.Hidden);
    }

    Color GetColor(MapNode n) => n.state switch
    {
        NodeState.Current   => ColCurrent,
        NodeState.Cleared   => ColCleared,
        NodeState.RouteOnly => ColRouteOnly,
        NodeState.Visible   => n.conditionType switch
        {
            NodeConditionType.None   => ColNone,
            NodeConditionType.Normal => ColNormal,
            NodeConditionType.Hard   => ColHard,
            _ => Color.white
        },
        _ => Color.white
    };

    List<List<MapNode>> CollectLayers(MapNode root)
    {
        var result = new List<List<MapNode>>();
        var queue = new Queue<MapNode>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            while (result.Count <= node.layer) result.Add(new());
            result[node.layer].Add(node);
            foreach (var child in node.children) queue.Enqueue(child);
        }
        return result;
    }

    static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float r = size / 2f - 1f;
        var pixels = tex.GetPixels32();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) <= r
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
