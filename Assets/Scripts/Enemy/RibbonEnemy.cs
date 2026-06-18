using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RibbonEnemy : EnemyBase
{
    [Header("Ribbon Motion")]
    [SerializeField, Min(0f)] float idleSwayAngle = 5f;
    [SerializeField, Min(0f)] float idlePulseAmount = 0.045f;
    [SerializeField, Min(0.1f)] float idleMotionSpeed = 1.5f;
    [SerializeField, Min(0.05f)] float attackWindupDuration = 0.22f;
    [SerializeField, Min(0.05f)] float fanSwingDuration = 0.28f;
    [SerializeField, Min(0.05f)] float bindThrustDuration = 0.24f;
    [SerializeField, Min(0f)] float moveSpeed = 0.85f;
    [SerializeField, Min(0.5f)] float preferredDistance = 3.8f;
    [SerializeField] Vector2 attackCooldownRange = new Vector2(2.2f, 3.4f);
    [SerializeField, Min(0.1f)] float telegraphDuration = 0.65f;
    [SerializeField, Min(0.5f)] float fanRadius = 3.2f;
    [SerializeField, Range(10f, 170f)] float fanAngle = 70f;
    [SerializeField, Min(0.5f)] float bindLength = 6.2f;
    [SerializeField, Min(0.05f)] float bindWidth = 0.45f;
    [SerializeField, Min(0f)] float bindDuration = 1f;
    [SerializeField, Min(1)] int fanDamage = 22;
    [SerializeField, Min(1)] int bindDamage = 12;
    [SerializeField] Color ribbonTelegraphColor = new Color(1f, 0.08f, 0.08f, 0.34f);

    Rigidbody2D rb;
    Transform player;
    Vector3 baseScale;
    Quaternion baseRotation;
    float motionTime;
    float nextAttackTime;
    bool isAttacking;

    protected override void Awake()
    {
        currentHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        EnsureCharacterShadow();
        EnsureCollider();
    }

    protected override void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;

        ResetAttackTimer();
    }

    public override void ApplyProfile(EnemyProfile profile)
    {
    }

    void FixedUpdate()
    {
        if (player == null || isAttacking)
            return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return;

        float distance = toPlayer.magnitude;
        Vector2 direction = distance > preferredDistance ? toPlayer.normalized : -toPlayer.normalized;
        float distanceBias = Mathf.Abs(distance - preferredDistance) < 0.35f ? 0f : 1f;
        rb.MovePosition(rb.position + direction * moveSpeed * distanceBias * Time.fixedDeltaTime);
    }

    protected override void Update()
    {
        UpdateIdleMotion();

        if (player == null || isAttacking)
            return;

        if (Time.time >= nextAttackTime)
        {
            if (Random.value < 0.55f)
                StartCoroutine(FanRoutine());
            else
                StartCoroutine(BindRoutine());
        }
    }

    IEnumerator FanRoutine()
    {
        isAttacking = true;
        Vector2 origin = rb.position;
        Vector2 direction = ((Vector2)player.position - origin).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        yield return StartCoroutine(WindupRoutine(direction, -28f));
        GameObject telegraph = EnemyTelegraph.CreateFan("RibbonFanTelegraph", origin, direction, fanRadius, fanAngle, ribbonTelegraphColor, 72);
        yield return new WaitForSeconds(telegraphDuration);
        yield return StartCoroutine(FanSwingRoutine(direction));

        PlayerDamageReceiver receiver = FindFirstObjectByType<PlayerDamageReceiver>();
        if (receiver != null && EnemyTelegraph.PointInFan(receiver.transform.position, origin, direction, fanRadius, fanAngle))
            receiver.TryTakePatternDamage(fanDamage);

        if (telegraph != null)
            Destroy(telegraph);

        ResetAttackTimer();
        isAttacking = false;
        RestoreMotionTransform();
    }

    IEnumerator BindRoutine()
    {
        isAttacking = true;
        Vector2 start = rb.position;
        Vector2 direction = ((Vector2)player.position - start).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        Vector2 center = start + direction * (bindLength * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 size = new Vector2(bindLength, bindWidth);
        yield return StartCoroutine(WindupRoutine(direction, 18f));
        GameObject telegraph = EnemyTelegraph.CreateBox("RibbonBindTelegraph", center, size, angle, ribbonTelegraphColor, 72);
        yield return EnemyTelegraph.Blink(telegraph, 1, telegraphDuration * 0.5f);
        yield return StartCoroutine(BindThrustRoutine(direction));

        PlayerDamageReceiver receiver = FindFirstObjectByType<PlayerDamageReceiver>();
        if (receiver != null && EnemyTelegraph.PointInOrientedBox(receiver.transform.position, center, size, angle))
        {
            receiver.TryTakePatternDamage(bindDamage);
            receiver.LockMovement(bindDuration);
        }

        if (telegraph != null)
            Destroy(telegraph);

        ResetAttackTimer();
        isAttacking = false;
        RestoreMotionTransform();
    }

    void UpdateIdleMotion()
    {
        if (isAttacking)
            return;

        motionTime += Time.deltaTime;
        float wave = Mathf.Sin(motionTime * Mathf.PI * 2f * idleMotionSpeed);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wave * idleSwayAngle);
        transform.localScale = new Vector3(
            baseScale.x * (1f + Mathf.Abs(wave) * idlePulseAmount),
            baseScale.y * (1f - Mathf.Abs(wave) * idlePulseAmount * 0.5f),
            baseScale.z);
    }

    IEnumerator WindupRoutine(Vector2 direction, float offsetAngle)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offsetAngle;
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = new Vector3(baseScale.x * 0.88f, baseScale.y * 1.18f, baseScale.z);
        float elapsed = 0f;
        while (elapsed < attackWindupDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, attackWindupDuration));
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FanSwingRoutine(Vector2 direction)
    {
        float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion startRotation = Quaternion.Euler(0f, 0f, centerAngle - 42f);
        Quaternion endRotation = Quaternion.Euler(0f, 0f, centerAngle + 42f);
        float elapsed = 0f;
        while (elapsed < fanSwingDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fanSwingDuration));
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localRotation = Quaternion.Lerp(startRotation, endRotation, eased);
            transform.localScale = new Vector3(baseScale.x * (1.05f + eased * 0.18f), baseScale.y * 0.72f, baseScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator BindThrustRoutine(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        float elapsed = 0f;
        while (elapsed < bindThrustDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, bindThrustDuration));
            float thrust = Mathf.Sin(t * Mathf.PI);
            transform.localScale = new Vector3(baseScale.x * (1f + thrust * 0.45f), baseScale.y * (1f - thrust * 0.25f), baseScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void RestoreMotionTransform()
    {
        transform.localRotation = baseRotation;
        transform.localScale = baseScale;
    }

    void ResetAttackTimer()
    {
        float min = Mathf.Max(0.1f, attackCooldownRange.x);
        float max = Mathf.Max(min, attackCooldownRange.y);
        nextAttackTime = Time.time + Random.Range(min, max);
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }
}
