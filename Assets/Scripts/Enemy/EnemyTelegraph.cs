using System.Collections;
using UnityEngine;

public static class EnemyTelegraph
{
    static Sprite squareSprite;
    static Sprite circleSprite;

    public static GameObject CreateBox(string name, Vector2 center, Vector2 size, float angleDegrees, Color fill, int sortingOrder = 60)
    {
        GameObject root = new GameObject(name);
        root.transform.position = new Vector3(center.x, center.y, 0f);
        root.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        SpriteRenderer fillRenderer = AddRect(root.transform, "Fill", Vector2.zero, size, fill, sortingOrder);
        fillRenderer.sortingOrder = sortingOrder;
        AddDashedRect(root.transform, size, WithAlpha(fill, 0.95f), sortingOrder + 1);
        return root;
    }

    public static GameObject CreateCircle(string name, Vector2 center, float radius, Color fill, int sortingOrder = 60)
    {
        GameObject root = new GameObject(name);
        root.transform.position = new Vector3(center.x, center.y, 0f);

        SpriteRenderer fillRenderer = root.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CircleSprite();
        fillRenderer.color = fill;
        fillRenderer.sortingOrder = sortingOrder;
        root.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        AddDashedCircle(root.transform, radius, WithAlpha(fill, 0.95f), sortingOrder + 1);
        return root;
    }

    public static GameObject CreateLine(string name, Vector2 start, Vector2 end, float width, Color color, int sortingOrder = 60)
    {
        Vector2 delta = end - start;
        Vector2 center = (start + end) * 0.5f;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        return CreateBox(name, center, new Vector2(delta.magnitude, width), angle, color, sortingOrder);
    }

    public static GameObject CreateFan(string name, Vector2 origin, Vector2 direction, float radius, float angleDegrees, Color color, int sortingOrder = 60)
    {
        GameObject root = new GameObject(name);
        root.transform.position = origin;

        int segments = 12;
        float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = centerAngle - angleDegrees * 0.5f;
        Vector2 previous = origin;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = (startAngle + angleDegrees * t) * Mathf.Deg2Rad;
            Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (i > 0)
                CreateLineSegment(root.transform, "FanEdge_" + i, previous - origin, point - origin, 0.12f, color, sortingOrder);

            previous = point;
        }

        CreateLineSegment(root.transform, "FanLeft", Vector2.zero, previous - origin, 0.12f, color, sortingOrder + 1);
        float rightAngle = startAngle * Mathf.Deg2Rad;
        Vector2 right = new Vector2(Mathf.Cos(rightAngle), Mathf.Sin(rightAngle)) * radius;
        CreateLineSegment(root.transform, "FanRight", Vector2.zero, right, 0.12f, color, sortingOrder + 1);
        return root;
    }

    public static IEnumerator Blink(GameObject target, int blinkCount, float interval)
    {
        if (target == null)
            yield break;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        int cycles = Mathf.Max(1, blinkCount) * 2;
        float wait = Mathf.Max(0.03f, interval);
        for (int i = 0; i < cycles; i++)
        {
            bool enabled = i % 2 == 0;
            for (int r = 0; r < renderers.Length; r++)
                if (renderers[r] != null)
                    renderers[r].enabled = enabled;

            yield return new WaitForSeconds(wait);
        }

        for (int r = 0; r < renderers.Length; r++)
            if (renderers[r] != null)
                renderers[r].enabled = true;
    }

    public static bool PointInOrientedBox(Vector2 point, Vector2 center, Vector2 size, float angleDegrees)
    {
        Quaternion inverse = Quaternion.Euler(0f, 0f, -angleDegrees);
        Vector2 local = inverse * (point - center);
        return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f;
    }

    public static bool PointInFan(Vector2 point, Vector2 origin, Vector2 direction, float radius, float angleDegrees)
    {
        Vector2 toPoint = point - origin;
        if (toPoint.sqrMagnitude > radius * radius)
            return false;

        if (toPoint.sqrMagnitude <= 0.0001f)
            return true;

        return Vector2.Angle(direction.normalized, toPoint.normalized) <= angleDegrees * 0.5f;
    }

    public static Sprite SquareSprite()
    {
        if (squareSprite != null)
            return squareSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return squareSprite;
    }

    public static Sprite CircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f - 1f;
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    static SpriteRenderer AddRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject rect = new GameObject(name);
        rect.transform.SetParent(parent, false);
        rect.transform.localPosition = position;
        rect.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = rect.AddComponent<SpriteRenderer>();
        renderer.sprite = SquareSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    static void AddDashedRect(Transform parent, Vector2 size, Color color, int sortingOrder)
    {
        float dashLength = 0.45f;
        float gap = 0.28f;
        AddDashedEdge(parent, new Vector2(-size.x * 0.5f, size.y * 0.5f), Vector2.right, size.x, dashLength, gap, color, sortingOrder);
        AddDashedEdge(parent, new Vector2(-size.x * 0.5f, -size.y * 0.5f), Vector2.right, size.x, dashLength, gap, color, sortingOrder);
        AddDashedEdge(parent, new Vector2(-size.x * 0.5f, -size.y * 0.5f), Vector2.up, size.y, dashLength, gap, color, sortingOrder);
        AddDashedEdge(parent, new Vector2(size.x * 0.5f, -size.y * 0.5f), Vector2.up, size.y, dashLength, gap, color, sortingOrder);
    }

    static void AddDashedCircle(Transform parent, float radius, Color color, int sortingOrder)
    {
        int dashCount = 24;
        for (int i = 0; i < dashCount; i += 2)
        {
            float a0 = i / (float)dashCount * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)dashCount * Mathf.PI * 2f;
            Vector2 start = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 end = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            CreateLineSegment(parent, "CircleDash_" + i, start, end, 0.08f, color, sortingOrder);
        }
    }

    static void AddDashedEdge(Transform parent, Vector2 start, Vector2 direction, float length, float dashLength, float gap, Color color, int sortingOrder)
    {
        int index = 0;
        float offset = 0f;
        while (offset < length)
        {
            float segment = Mathf.Min(dashLength, length - offset);
            Vector2 center = start + direction * (offset + segment * 0.5f);
            Vector2 size = Mathf.Abs(direction.x) > 0f ? new Vector2(segment, 0.08f) : new Vector2(0.08f, segment);
            AddRect(parent, "Dash_" + index, center, size, color, sortingOrder);
            offset += dashLength + gap;
            index++;
        }
    }

    static void CreateLineSegment(Transform parent, string name, Vector2 start, Vector2 end, float width, Color color, int sortingOrder)
    {
        Vector2 delta = end - start;
        Vector2 center = (start + end) * 0.5f;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        SpriteRenderer renderer = AddRect(parent, name, center, new Vector2(delta.magnitude, width), color, sortingOrder);
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
