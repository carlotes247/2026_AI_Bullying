using System;
using UnityEngine;

/// <summary>
/// A physics projectile that fires once toward the walker ragdoll (with optional
/// spread) and reports the first agent collision as a gameplay Push.
/// No homing — it travels as a regular physics object after launch.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class AgentSeekingProjectile : MonoBehaviour
{
    public static event Action<AgentSeekingProjectile> AgentHit;

    [Header("Launch")]
    [Min(0f)] public float launchSpeed = 12f;
    [Range(0f, 60f)] public float spreadAngle = 15f;
    [Min(0.1f)] public float lifetimeSeconds = 5f;

    Rigidbody body;
    Transform target;
    bool hitReported;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetimeSeconds);
    }

    void Start()
    {
        ResolveTarget();
        Vector3 dir = target != null
            ? (target.position - transform.position).normalized
            : transform.forward;

        body.linearVelocity = ApplyConeSpread(dir, spreadAngle) * launchSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    static Vector3 ApplyConeSpread(Vector3 direction, float halfAngleDeg)
    {
        if (halfAngleDeg <= 0f) return direction;
        // Pick a random perpendicular axis, then tilt direction by a random angle within the cone.
        Vector3 perp = Vector3.Cross(direction,
            Mathf.Abs(direction.x) < 0.9f ? Vector3.right : Vector3.up).normalized;
        perp = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), direction) * perp;
        return Quaternion.AngleAxis(UnityEngine.Random.Range(0f, halfAngleDeg), perp) * direction;
    }

    void ResolveTarget()
    {
        if (target != null) return;
        WalkerAgent walker = FindFirstObjectByType<WalkerAgent>(FindObjectsInactive.Include);
        if (walker != null)
            target = walker.chest != null ? walker.chest : walker.hips;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hitReported || !collision.transform.CompareTag("agent"))
            return;

        hitReported = true;
        AgentHit?.Invoke(this);
        Destroy(gameObject, 0.15f);
    }
}
