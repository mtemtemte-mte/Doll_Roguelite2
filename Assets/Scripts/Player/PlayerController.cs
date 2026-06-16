using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField, Range(0f, 1f)] float missingLegSpeedMultiplier = 0.5f;
    [SerializeField] Color bodyColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer leftArmRenderer;
    [SerializeField] SpriteRenderer rightArmRenderer;
    [SerializeField] Sprite upSprite;
    [SerializeField] Sprite downSprite;
    [SerializeField] Sprite leftSprite;
    [SerializeField] Sprite rightSprite;
    [SerializeField] Sprite[] frontWalkBodyFrames;
    [SerializeField] Sprite[] frontWalkArmFrames;
    [SerializeField, Min(1f)] float frontWalkFramesPerSecond = 8f;

    Rigidbody2D rb;
    Vector2 moveInput;
    bool forwardWalkPressed;
    FacingDirection facingDirection = FacingDirection.Down;
    float facingLockTimer;
    float walkAnimationTime;
    int lastWalkFrame = -1;

    enum FacingDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        LoadDefaultSpritesIfMissing();
        BuildLayeredRenderers();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            ApplyFacingSprite();
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f, v = 0f;
        bool leftPressed = kb.aKey.isPressed;
        bool rightPressed = kb.dKey.isPressed;
        bool downPressed = kb.sKey.isPressed;
        bool upPressed = kb.wKey.isPressed;

        if (leftPressed) h -= 1f;
        if (rightPressed) h += 1f;
        if (downPressed) v -= 1f;
        if (upPressed) v += 1f;
        moveInput = new Vector2(h, v).normalized;
        forwardWalkPressed = downPressed && moveInput.y < -0.01f;

        facingLockTimer -= Time.deltaTime;

        if (moveInput != Vector2.zero && facingLockTimer <= 0f)
            SetFacingFromMoveInput();

        UpdateWalkAnimationTime();
        ApplyFacingSprite();
    }

    void FixedUpdate()
    {
        float speed = moveSpeed;
        var state = BodyConditionUtility.CurrentState();
        if (state != null && (!state.legLeft || !state.legRight))
            speed *= missingLegSpeedMultiplier;

        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
    }

    public void FaceDirection(Vector2 direction)
    {
        FaceDirection(direction, 0f);
    }

    public void FaceDirection(Vector2 direction, float lockDuration)
    {
        if (direction == Vector2.zero)
            return;

        facingLockTimer = Mathf.Max(facingLockTimer, lockDuration);

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            SetFacing(direction.x < 0f ? FacingDirection.Left : FacingDirection.Right);
        else
            SetFacing(direction.y < 0f ? FacingDirection.Down : FacingDirection.Up);
    }

    bool SetFacing(FacingDirection direction)
    {
        if (facingDirection == direction)
            return false;

        facingDirection = direction;
        ApplyFacingSprite();
        return true;
    }

    void SetFacingFromMoveInput()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            SetFacing(moveInput.x < 0f ? FacingDirection.Left : FacingDirection.Right);
        else
            SetFacing(moveInput.y < 0f ? FacingDirection.Down : FacingDirection.Up);
    }

    void ApplyFacingSprite()
    {
        if (spriteRenderer == null)
            return;

        if (ShouldUseFrontWalkAnimation())
        {
            ApplyFrontWalkFrame();
            return;
        }

        lastWalkFrame = -1;
        SetArmRenderersVisible(false, false);

        Sprite nextSprite = facingDirection switch
        {
            FacingDirection.Up => upSprite,
            FacingDirection.Left => leftSprite,
            FacingDirection.Right => rightSprite,
            _ => downSprite
        };

        if (nextSprite != null)
            spriteRenderer.sprite = nextSprite;
    }

    void LoadDefaultSpritesIfMissing()
    {
        if (upSprite == null)
            upSprite = LoadPlayerSprite("behind", "Player_up");
        if (downSprite == null)
            downSprite = LoadPlayerSprite("front", "Player_down");
        if (leftSprite == null)
            leftSprite = LoadPlayerSprite("left", "Player_left");
        if (rightSprite == null)
            rightSprite = LoadPlayerSprite("right", "Player_right");
        if (frontWalkBodyFrames == null || frontWalkBodyFrames.Length == 0)
            frontWalkBodyFrames = LoadPlayerSprites("front_walk_body");
        if (frontWalkArmFrames == null || frontWalkArmFrames.Length == 0)
            frontWalkArmFrames = LoadPlayerSprites("front_walk_arm");
    }

    Sprite LoadPlayerSprite(string spriteName, string fallbackName)
    {
        Sprite sprite = LoadFirstSprite("Sprites/Player/" + spriteName);
        if (sprite != null)
            return sprite;

#if UNITY_EDITOR
        sprite = LoadEditorSprite("Assets/Sprites/Player/" + spriteName + ".png");
        if (sprite != null)
            return sprite;

        sprite = LoadEditorSprite("Assets/TextMesh Pro/Sprites/Player/" + spriteName + ".png");
        if (sprite != null)
            return sprite;
#endif

        return LoadFirstSprite("Sprites/Player/" + fallbackName);
    }

    void BuildLayeredRenderers()
    {
        if (spriteRenderer == null)
            return;

        leftArmRenderer = EnsureArmRenderer(leftArmRenderer, "PlayerArm_Left");
        rightArmRenderer = EnsureArmRenderer(rightArmRenderer, "PlayerArm_Right");
        SetArmRenderersVisible(false, false);
    }

    SpriteRenderer EnsureArmRenderer(SpriteRenderer renderer, string objectName)
    {
        if (renderer == null)
        {
            Transform existing = transform.Find(objectName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = go.AddComponent<SpriteRenderer>();
        }

        renderer.color = Color.white;
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        renderer.sharedMaterial = spriteRenderer.sharedMaterial;
        return renderer;
    }

    void UpdateWalkAnimationTime()
    {
        if (ShouldUseFrontWalkAnimation())
            walkAnimationTime += Time.deltaTime;
        else
            walkAnimationTime = 0f;
    }

    bool ShouldUseFrontWalkAnimation()
    {
        return facingDirection == FacingDirection.Down
            && forwardWalkPressed
            && frontWalkBodyFrames != null
            && frontWalkBodyFrames.Length > 0
            && frontWalkArmFrames != null
            && frontWalkArmFrames.Length >= frontWalkBodyFrames.Length * 2;
    }

    void ApplyFrontWalkFrame()
    {
        int frame = Mathf.FloorToInt(walkAnimationTime * frontWalkFramesPerSecond) % frontWalkBodyFrames.Length;
        if (frame != lastWalkFrame)
        {
            spriteRenderer.sprite = frontWalkBodyFrames[frame];
            ApplyArmFrame(frame, leftArmRenderer, frame * 2, BodySlot.ArmLeft);
            ApplyArmFrame(frame, rightArmRenderer, frame * 2 + 1, BodySlot.ArmRight);
            lastWalkFrame = frame;
        }
    }

    void ApplyArmFrame(int bodyFrame, SpriteRenderer renderer, int armFrame, BodySlot slot)
    {
        if (renderer == null)
            return;

        bool visible = BodyConditionUtility.HasPart(slot);
        Sprite bodySprite = frontWalkBodyFrames[bodyFrame];
        Sprite armSprite = frontWalkArmFrames[armFrame];

        renderer.sprite = armSprite;
        renderer.transform.localPosition = CalculateArmOffset(bodySprite, armSprite, bodyFrame);
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        renderer.enabled = visible && armSprite != null;
    }

    Vector3 CalculateArmOffset(Sprite bodySprite, Sprite armSprite, int frame)
    {
        if (bodySprite == null || armSprite == null || bodySprite.texture == null)
            return Vector3.zero;

        float pixelsPerUnit = bodySprite.pixelsPerUnit;
        if (pixelsPerUnit <= 0f)
            pixelsPerUnit = 100f;

        float frameWidth = bodySprite.texture.width / Mathf.Max(1f, frontWalkBodyFrames.Length);
        Vector2 bodyPivot = SpritePivotInFrame(bodySprite, frame, frameWidth);
        Vector2 armPivot = SpritePivotInFrame(armSprite, frame, frameWidth);
        Vector2 offset = (armPivot - bodyPivot) / pixelsPerUnit;
        return new Vector3(offset.x, offset.y, 0f);
    }

    Vector2 SpritePivotInFrame(Sprite sprite, int frame, float frameWidth)
    {
        Rect rect = sprite.rect;
        return new Vector2(rect.x - frame * frameWidth + sprite.pivot.x, rect.y + sprite.pivot.y);
    }

    void SetArmRenderersVisible(bool leftVisible, bool rightVisible)
    {
        if (leftArmRenderer != null)
            leftArmRenderer.enabled = leftVisible;
        if (rightArmRenderer != null)
            rightArmRenderer.enabled = rightVisible;
    }

    Sprite[] LoadPlayerSprites(string spriteName)
    {
        Sprite[] sprites = SortSprites(Resources.LoadAll<Sprite>("Sprites/Player/" + spriteName));
        if (sprites.Length > 0)
            return sprites;

        Sprite sprite = Resources.Load<Sprite>("Sprites/Player/" + spriteName);
        if (sprite != null)
            return new[] { sprite };

#if UNITY_EDITOR
        sprites = LoadEditorSprites("Assets/Sprites/Player/" + spriteName + ".png");
        if (sprites.Length > 0)
            return sprites;
#endif

        return new Sprite[0];
    }

    Sprite[] SortSprites(Sprite[] sprites)
    {
        return sprites.OrderBy(sprite => sprite.rect.x)
            .ThenBy(sprite => sprite.rect.y)
            .ThenBy(sprite => sprite.name)
            .ToArray();
    }

#if UNITY_EDITOR
    Sprite LoadEditorSprite(string assetPath)
    {
        Sprite[] sprites = LoadEditorSprites(assetPath);
        if (sprites.Length > 0)
            return sprites[0];

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    Sprite[] LoadEditorSprites(string assetPath)
    {
        return SortSprites(AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray());
    }
#endif

    Sprite LoadFirstSprite(string resourcePath)
    {
        Sprite[] sprites = SortSprites(Resources.LoadAll<Sprite>(resourcePath));
        if (sprites.Length > 0)
            return sprites[0];

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;

        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
    }
}
