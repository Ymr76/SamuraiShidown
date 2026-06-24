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
    
   void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); 
        
        player = GameObject.Find("Player").transform;
        playerScript = player.GetComponent<PlayerMovement>();

        // --- NEW: STAGGER THE MOB ---
        // Pick a random speed between 2.0 and 3.5
        moveSpeed = Random.Range(2.0f, 3.5f); 
        
        // Pick a random stopping distance so they don't all stand on the exact same pixel
        attackRange = Random.Range(1.2f, 2.2f); 

        UpdateUI();
    }
    void Update()
    {
        UpdateUI();

        if (currentState == EnemyState.Stunned || currentHealth <= 0) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            AttackPlayer();
        }
        else if (distanceToPlayer <= awareRange)
        {
            currentState = EnemyState.Aware;
            ChasePlayer();
        }
        else
        {
            currentState = EnemyState.Idle;
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

        // --- FIXED FLIP LOGIC (Preserves your custom size!) ---
        Vector3 currentScale = transform.localScale;
        if (direction.x > 0) 
        {
            currentScale.x = Mathf.Abs(currentScale.x); // Face Right
        }
        else if (direction.x < 0) 
        {
            currentScale.x = -Mathf.Abs(currentScale.x); // Face Left
        }
        transform.localScale = currentScale;
    }

    void AttackPlayer()
    {
        rb.velocity = new Vector2(0, rb.velocity.y); 

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
            rb.velocity = Vector2.zero;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0) Die();
        
        UpdateUI();
    }

    void Die()
    {
        currentState = EnemyState.Stunned; 
        GetComponent<Collider2D>().enabled = false;
        if(hpText != null) hpText.text = "DEAD";
        this.enabled = false;
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