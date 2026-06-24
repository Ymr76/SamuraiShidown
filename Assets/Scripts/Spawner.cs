using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    
    public float spawnInterval = 4f; // Time between spawns

    [Header("Map Bounds")]
    [Tooltip("Enemies are clamped to spawn inside the map walls so they can always reach the player.")]
    public float minSpawnX = -20f;
    public float maxSpawnX = 27f;

    [Header("Spawn Placement")]
    [Tooltip("Enemies will never spawn closer than this (along X) to the player.")]
    public float minDistanceFromPlayer = 7f;
    [Tooltip("Vertical offset above the player's position where enemies drop in.")]
    public float spawnYOffset = 2f;

    private float timer;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (player == null || enemyPrefab == null) return;

        float spawnX = PickSpawnX();
        Vector3 spawnPos = new Vector3(spawnX, player.position.y + spawnYOffset, 0);

        // Clone the enemy prefab at that location
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    /// <summary>
    /// Picks a random X within the map bounds that is at least
    /// minDistanceFromPlayer away from the player on either side.
    /// </summary>
    float PickSpawnX()
    {
        float playerX = player.position.x;

        // The two valid zones: left of the player and right of the player,
        // each starting minDistanceFromPlayer away and stopping at the map walls.
        float leftZoneMax = playerX - minDistanceFromPlayer;   // spawn anywhere in [minSpawnX, leftZoneMax]
        float rightZoneMin = playerX + minDistanceFromPlayer;  // spawn anywhere in [rightZoneMin, maxSpawnX]

        bool leftValid = leftZoneMax > minSpawnX;
        bool rightValid = rightZoneMin < maxSpawnX;

        if (leftValid && rightValid)
        {
            // Both sides have room: weight the random pick by each zone's width
            // so spawns are evenly distributed across all valid space.
            float leftWidth = leftZoneMax - minSpawnX;
            float rightWidth = maxSpawnX - rightZoneMin;
            if (Random.value < leftWidth / (leftWidth + rightWidth))
                return Random.Range(minSpawnX, leftZoneMax);
            return Random.Range(rightZoneMin, maxSpawnX);
        }
        if (rightValid)
        {
            return Random.Range(rightZoneMin, maxSpawnX);
        }
        if (leftValid)
        {
            return Random.Range(minSpawnX, leftZoneMax);
        }

        // Fallback (player pinned against a wall / map smaller than the buffer):
        // spawn at whichever wall is farther from the player.
        float distToMin = Mathf.Abs(playerX - minSpawnX);
        float distToMax = Mathf.Abs(maxSpawnX - playerX);
        return distToMax >= distToMin ? maxSpawnX : minSpawnX;
    }
}