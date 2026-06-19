using System;
using Bully;
using UnityEngine;

/// <summary>
/// Spawns a random volley of cube/sphere physics projectiles on a valid click.
/// Each projectile fires toward the walker ragdoll and then travels as pure physics.
/// </summary>
public class PointerPhysics : MonoBehaviour
{
    public static event Action PlayerFired;

    [Header("Projectile prefabs")]
    public GameObject spherePrefab;
    public GameObject cubePrefab;

    [Header("Volley")]
    public Vector2Int projectilesPerClick = new Vector2Int(1, 3);
    [Min(0f)] public float clickCooldownSeconds = 3f;
    [Min(0.1f)] public float spawnDistanceFromCamera = 3f;
    [Min(0f)] public float spawnSpread = 1.25f;

    public float CooldownRemaining => Mathf.Max(0f, nextAllowedClickAt - Time.time);

    Camera mainCam;
    GameFlow gameFlow;
    Transform ragdollTarget;
    float nextAllowedClickAt;

    void Awake()
    {
        mainCam = Camera.main;
        gameFlow = FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);
        ResolveTarget();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (gameFlow != null && (!gameFlow.IsGameplayActive || gameFlow.IsGameOver))
            return;
        if (Time.time < nextAllowedClickAt)
            return;

        SpawnVolley();
        nextAllowedClickAt = Time.time + clickCooldownSeconds;
        PlayerFired?.Invoke();
    }

    void SpawnVolley()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null)
            return;

        ResolveTarget();

        int minimum = Mathf.Min(projectilesPerClick.x, projectilesPerClick.y);
        int maximum = Mathf.Max(projectilesPerClick.x, projectilesPerClick.y);
        int count = UnityEngine.Random.Range(Mathf.Max(1, minimum), Mathf.Max(1, maximum) + 1);

        Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
        Vector3 volleyCentre = mouseRay.origin + mouseRay.direction * spawnDistanceFromCamera;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = UnityEngine.Random.value < 0.5f ? spherePrefab : cubePrefab;
            if (prefab == null)
                prefab = spherePrefab != null ? spherePrefab : cubePrefab;
            if (prefab == null)
                continue;

            Vector2 spread = UnityEngine.Random.insideUnitCircle * spawnSpread;
            Vector3 spawnPosition = volleyCentre +
                mainCam.transform.right * spread.x +
                mainCam.transform.up * spread.y;
            Quaternion rotation = UnityEngine.Random.rotation;
            GameObject projectileObject = Instantiate(prefab, spawnPosition, rotation);
            AgentSeekingProjectile projectile =
                projectileObject.GetComponent<AgentSeekingProjectile>();
            if (projectile != null)
                projectile.SetTarget(ragdollTarget);
        }
    }

    void ResolveTarget()
    {
        if (ragdollTarget != null)
            return;

        WalkerAgent walker = FindFirstObjectByType<WalkerAgent>(FindObjectsInactive.Include);
        if (walker != null)
            ragdollTarget = walker.chest != null ? walker.chest : walker.hips;
    }

    // Kept for the existing Interactable prefab; pickup is intentionally disabled.
    public void Grab(Interactable interactable) { }
    public void Release(Interactable interactable) { }
}
