using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates enemies so they queue up around the player instead of stacking.
/// Only a limited number (maxAttackers) may engage at melee range at once;
/// the rest line up behind at spaced standoff distances.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Engagement Tuning")]
    [Tooltip("How many enemies may attack the player at the same time.")]
    public int maxAttackers = 1;

    [Tooltip("Stop distance (along X) from the player for the front attacker.")]
    public float engageDistance = 1.5f;

    [Tooltip("Horizontal gap between each queued enemy.")]
    public float queueSpacing = 1.8f;

    private readonly List<Enemy> enemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(Enemy e)
    {
        if (e != null && !enemies.Contains(e)) enemies.Add(e);
    }

    public void Unregister(Enemy e)
    {
        enemies.Remove(e);
    }

    /// <summary>
    /// Computes the engagement info for a given enemy: how far from the player it should
    /// stop, and whether it is allowed to attack (front of the queue) or must wait.
    /// Queueing is computed per side of the player (left / right) by distance.
    /// </summary>
    public void GetEngagement(Enemy self, Transform player, out float stopDistance, out bool canAttack)
    {
        float selfSide = Mathf.Sign(self.transform.position.x - player.position.x);
        float selfDist = Mathf.Abs(self.transform.position.x - player.position.x);
        int selfIndex = enemies.IndexOf(self);

        int rank = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy other = enemies[i];
            if (other == null || other == self || !other.IsAlive) continue;
            if (Mathf.Sign(other.transform.position.x - player.position.x) != selfSide) continue;

            float otherDist = Mathf.Abs(other.transform.position.x - player.position.x);
            // Closer enemies rank ahead; ties broken by registration order for stability.
            bool ahead = otherDist < selfDist - 0.001f ||
                         (Mathf.Abs(otherDist - selfDist) <= 0.001f && i < selfIndex);
            if (ahead) rank++;
        }

        canAttack = rank < Mathf.Max(1, maxAttackers);
        stopDistance = engageDistance + rank * queueSpacing;
    }
}
