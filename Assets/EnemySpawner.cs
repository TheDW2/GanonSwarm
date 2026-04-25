using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    [Header("Wave Timing")]
    public float initialDelay = 5f;        // Delay before first wave
    public float waveDuration = 30f;       // How long each wave lasts
    public float restDuration = 10f;       // Rest period between waves

    [Header("Spawn Rate")]
    public float baseSpawnInterval = 2f;   // Seconds between spawns in wave 1
    public float spawnIntervalDecrement = 0.15f; // Spawn interval gets faster each wave (min capped)
    public float minSpawnInterval = 0.4f;  // Fastest possible spawn interval

    [Header("Enemies Per Spawn")]
    public int baseEnemiesPerSpawn = 1;    // Enemies spawned at once in wave 1
    public int enemiesPerSpawnIncrement = 1; // Extra enemies added per wave
    public int maxEnemiesPerSpawn = 6;     // Cap

    [Header("Enemy Mix")]
    [Range(0f, 1f)]
    public float meleeSpawnChance = 0.65f; // Chance of spawning melee vs ranged

    [Header("Spawn Positioning")]
    public float spawnPadding = 1.5f;      // How far outside camera edge enemies spawn

    // ─────────────────────────────────────────
    private Camera mainCamera;
    private Transform player;
    private int currentWave = 0;
    private bool isSpawning = false;

    // Track last side used to reduce same-side clustering
    private int lastSideIndex = -1;

    void Start()
    {
        mainCamera = Camera.main;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        StartCoroutine(SpawnerLoop());
    }

    IEnumerator SpawnerLoop()
    {
        // Initial delay before first wave
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            currentWave++;
            Debug.Log($"Wave {currentWave} started!");

            // Start spawning
            isSpawning = true;
            StartCoroutine(SpawnWave(currentWave));

            // Wait for wave to finish
            yield return new WaitForSeconds(waveDuration);

            // Stop spawning
            isSpawning = false;
            Debug.Log($"Wave {currentWave} ended. Rest for {restDuration}s.");

            yield return new WaitForSeconds(restDuration);
        }
    }

    IEnumerator SpawnWave(int wave)
    {
        float spawnInterval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (spawnIntervalDecrement * (wave - 1)));
        int enemiesPerSpawn = Mathf.Min(maxEnemiesPerSpawn, baseEnemiesPerSpawn + (enemiesPerSpawnIncrement * (wave - 1)));

        while (isSpawning)
        {
            for (int i = 0; i < enemiesPerSpawn; i++)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        if (player == null || mainCamera == null) return;

        Vector2 spawnPos = GetSpawnPosition();

        // Pick enemy type
        GameObject prefab = (Random.value <= meleeSpawnChance) ? meleeEnemyPrefab : rangedEnemyPrefab;
        if (prefab == null) return;

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    Vector2 GetSpawnPosition()
    {
        // Get camera bounds in world space
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        Vector2 camCenter = mainCamera.transform.position;

        // 4 sides: 0 = top, 1 = bottom, 2 = left, 3 = right
        // Weight sides so same side as last spawn is less likely
        int side = PickSide();
        lastSideIndex = side;

        float spawnX, spawnY;

        switch (side)
        {
            case 0: // Top
                spawnX = Random.Range(camCenter.x - camWidth, camCenter.x + camWidth);
                spawnY = camCenter.y + camHeight + spawnPadding;
                break;
            case 1: // Bottom
                spawnX = Random.Range(camCenter.x - camWidth, camCenter.x + camWidth);
                spawnY = camCenter.y - camHeight - spawnPadding;
                break;
            case 2: // Left
                spawnX = camCenter.x - camWidth - spawnPadding;
                spawnY = Random.Range(camCenter.y - camHeight, camCenter.y + camHeight);
                break;
            case 3: // Right
                spawnX = camCenter.x + camWidth + spawnPadding;
                spawnY = Random.Range(camCenter.y - camHeight, camCenter.y + camHeight);
                break;
            default:
                spawnX = camCenter.x;
                spawnY = camCenter.y;
                break;
        }

        return new Vector2(spawnX, spawnY);
    }

    int PickSide()
    {
        // Build weighted list — reduce weight of last used side
        float[] weights = new float[4];
        for (int i = 0; i < 4; i++)
            weights[i] = (i == lastSideIndex) ? 0.1f : 1f;

        float total = 0f;
        foreach (float w in weights) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < 4; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return i;
        }

        return Random.Range(0, 4);
    }

    // Draw camera spawn boundary in Scene view
    void OnDrawGizmos()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float camHeight = mainCamera.orthographicSize + spawnPadding;
        float camWidth = (mainCamera.orthographicSize * mainCamera.aspect) + spawnPadding;
        Vector3 center = mainCamera.transform.position;
        center.z = 0f;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawWireCube(center, new Vector3(camWidth * 2f, camHeight * 2f, 0f));
    }
}