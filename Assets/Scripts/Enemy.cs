using UnityEngine;
using TMPro; 

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Aware, Attacking, Stunned }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("AI Settings")]
    public Transform player;
    public float awareRange = 8f;   
    public float attackRange = 1.5f; 
    public float moveSpeed = 3f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("UI")]
    public Transform uiCanvas; // NEW: We need to grab the canvas to stop it from flipping!
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI stateText;

    private Rigidbody2D rb;
    private PlayerMovement playerScript;
    private Animator anim; // NEW: We need to talk to the enemy's Animator

    private bool isDead = false;
    public bool IsAlive => !isDead;

   void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); 
        
        player = GameObject.Find("Player").transform;
        playerScript = player.GetComponent<PlayerMovement>();

        // --- Stagger the mob movement speed so they don't move in lockstep ---
        moveSpeed = Random.Range(2.0f, 3.5f); 

        // Register with the queue coordinator so we line up instead of stacking.
        if (EnemyManager.Instance != null) EnemyManager.Instance.Register(this);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null) EnemyManager.Instance.Unregister(this);
    }

    void Update()
    {
        UpdateUI();

        if (currentState == EnemyState.Stunned || currentHealth <= 0) return;

        float dx = player.position.x - transform.position.x;
        float horizontalDist = Mathf.Abs(dx);
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Out of awareness range: idle.
        if (distanceToPlayer > awareRange)
        {
            currentState = EnemyState.Idle;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Always face the player once aware.
        FacePlayer(dx);

        // Ask the coordinator where we should stop and whether we may attack.
        float stopDistance = attackRange;
        bool canAttack = true;
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.GetEngagement(this, player, out stopDistance, out canAttack);

        if (horizontalDist > stopDistance + 0.05f)
        {
            // Walk toward our assigned slot (front of queue = melee range, others stand back).
            currentState = EnemyState.Aware;
            float dir = Mathf.Sign(dx);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Reached our slot: hold position.
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (canAttack)
            {
                // Front of the queue and at melee range -> attack.
                currentState = EnemyState.Attacking;
                AttackPlayer();
            }
            else
            {
                // Waiting in the queue behind the active attacker.
                currentState = EnemyState.Aware;
            }
        }
    }

    void FacePlayer(float dx)
    {
        Vector3 currentScale = transform.localScale;
        if (dx > 0)
        {
            currentScale.x = Mathf.Abs(currentScale.x); // Face Right
        }
        else if (dx < 0)
        {
            currentScale.x = -Mathf.Abs(currentScale.x); // Face Left
        }
        transform.localScale = currentScale;
    }

    void AttackPlayer()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            
            // Just trigger the animation here! We removed the damage math.
            anim.SetTrigger("Attack"); 
        }
    }

    public void ExecuteHit()
    {
        if (playerScript.isParrying)
        {
            Debug.Log("Enemy was parried!");
            playerScript.TriggerParryFlash(); 
            StartCoroutine(StunRoutine());
        }
        else
        {
            // --- NEW: Actually hurt the player! ---
            Debug.Log("Enemy hit the Player!");
            playerScript.TakeDamage(20); // Deals 20 damage per swing
        }
    }

    System.Collections.IEnumerator StunRoutine()
    {
        currentState = EnemyState.Stunned;
        
        Vector2 pushBack = (transform.position - player.position).normalized;
        rb.AddForce(new Vector2(pushBack.x, 0) * 5f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(2f); 
        
        currentState = EnemyState.Idle; 
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
    {
        currentHealth -= damage;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0) Die();
        
        UpdateUI();
    }

    void Die()
    {
        isDead = true;
        currentState = EnemyState.Stunned; 
        GetComponent<Collider2D>().enabled = false;
        if(hpText != null) hpText.text = "DEAD";
        this.enabled = false;

        // Free our queue slot so the next enemy can advance and attack.
        if (EnemyManager.Instance != null) EnemyManager.Instance.Unregister(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }
    }

    void UpdateUI()
    {
        if (stateText != null) stateText.text = currentState.ToString();
        if (hpText != null && currentHealth > 0) hpText.text = "HP: " + currentHealth;

        // --- NEW: COUNTER-FLIP THE TEXT ---
        if (uiCanvas != null)
        {
            Vector3 canvasScale = uiCanvas.localScale;
            
            // If the samurai is flipped backwards, we flip the canvas backwards so it cancels out!
            if (transform.localScale.x < 0)
            {
                canvasScale.x = -Mathf.Abs(canvasScale.x);
            }
            else
            {
                canvasScale.x = Mathf.Abs(canvasScale.x);
            }
            
            uiCanvas.localScale = canvasScale;
        }
    }
}