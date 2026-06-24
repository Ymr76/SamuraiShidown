using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    
    public float spawnInterval = 4f; // Time between spawns
    public float spawnDistanceX = 12f; // How far to the right they spawn
    
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
        if (player == null) return;

        // Calculate a position way out to the right of the player
        Vector3 spawnPos = new Vector3(player.position.x + spawnDistanceX, player.position.y + 2f, 0);

        // Clone the enemy prefab at that location
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}