using UnityEngine;
using System.Collections; // We need this for Coroutines (timers)
using UnityEngine.UI; // Needed for UI images

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float knockbackForce = 10f;
    
public Image whiteFlashImage;
    [Header("Combat Settings")]
    public Transform attackPoint;
    public float attackRange = 0.7f;
    public LayerMask enemyLayers;
    
    [Header("Parry Settings")]
    public float parryDuration = 0.4f; // How long the parry window stays active
    public bool isParrying = false;    // Tracks if we are currently parrying
    [Header("Parry Effects")]
    public GameObject sparkPrefab;
    public AudioClip parrySound;
    public AudioSource sfxPlayer;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr; // Used for temporary visual feedback
    private float moveInput;

    [Header("Player Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public Image healthBarFill; // Drag your RedFill here!
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); 
        
        
        // NEW: Set health to full at the start
        currentHealth = maxHealth; 
    }

    void Update()
    {
        // 1. If we are parrying, stop all other actions immediately
        if (isParrying)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop sliding
            return; // Skip the rest of the Update loop
        }

        // 2. Normal Movement Logic
        moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); 
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);  
        }

        anim.SetFloat("Speed", Mathf.Abs(moveInput)); 
        //parry sounds and checks
        if (Input.GetMouseButtonDown(1) && !isParrying) 
        {
            StartCoroutine(ParryRoutine());
        }
        
        System.Collections.IEnumerator ParryRoutine()
        {
        isParrying = true;
        anim.SetTrigger("Parry"); // Play the block animation

        yield return new WaitForSeconds(parryDuration); 

        isParrying = false; // Parry window closes
        }
        // 3. Attack Input
        if (Input.GetMouseButtonDown(0))
        {
            anim.SetTrigger("Attack");
            Attack(); 
        }

        // 4. Parry Input (Right Click)
        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(ParryRoutine());
        }
    }
void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach(Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemyScript = enemyCollider.GetComponent<Enemy>();
            
            if (enemyScript != null)
            {
                // 1. Calculate the angle from the player to the enemy
                Vector2 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;

                // 2. We only want them to fly backwards, not up or down, so we lock the Y axis to 0
                knockbackDirection.y = 0; 

                // 3. Send the damage AND the knockback information to the enemy
                enemyScript.TakeDamage(34, knockbackDirection, knockbackForce);
            }
        }
    }
    public void TakeDamage(int damage)
    {
        if (isParrying) return; // Invincible while parrying!

        // Subtract the damage from the health pool
        currentHealth -= damage;
        
        // Print to the console so we know it worked!
        Debug.Log("Player took " + damage + " damage! HP left: " + currentHealth);

        // Update the red bar visually
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }

        // Kill the player if health drops to zero
        if (currentHealth <= 0)
        {
            Debug.Log("GAME OVER - Player Died!");
            this.enabled = false; 
        }
    }
    // --- NEW PARRY LOGIC ---
    IEnumerator ParryRoutine()
    {
        // Play the sound
        if (sfxPlayer != null && parrySound != null)
        {
            sfxPlayer.PlayOneShot(parrySound);
        }
        
        isParrying = true;
        anim.SetTrigger("Parry");

        // --- NEW: SPAWN SPARKS ---
        // We spawn the sparks at the player's position
        if (sparkPrefab != null)
        {
            // Spawn the sparks slightly in front of the player
            // (transform.localScale.x helps it spawn on the correct side depending on which way you face)
            Vector3 sparkPos = transform.position + new Vector3(transform.localScale.x * 0.8f, 0, 0); 
            
            // Create the spark clone in the world
            GameObject sparks = Instantiate(sparkPrefab, sparkPos, Quaternion.identity);
            
            // Automatically delete the sparks after 1 second so they don't lag your game forever
            Destroy(sparks, 1f); 
        }

        // Turn cyan so you know the parry window is active
        sr.color = Color.cyan; 

        // Wait for the parry duration to finish
        yield return new WaitForSeconds(parryDuration);

        // Reset back to normal
        sr.color = Color.white;
        isParrying = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }


    public void TriggerParryFlash()
    {
        // 1. Flash the screen white
        StartCoroutine(FlashRoutine());

        // 2. Play the Clash Sound WITH DEBUGGING
        if (sfxPlayer != null && parrySound != null)
        {
            Debug.Log("SOUND CHECK: Everything is loaded. Playing sound now!");
            sfxPlayer.PlayOneShot(parrySound);
        }
        else
        {
            Debug.Log("SOUND ERROR: Something is missing! sfxPlayer is: " + sfxPlayer + " and parrySound is: " + parrySound);
        }

        // 3. Spawn the Sparks
        if (sparkPrefab != null)
        {
            Vector3 sparkPos = transform.position + new Vector3(transform.localScale.x * 0.8f, 0, 0); 
            GameObject sparks = Instantiate(sparkPrefab, sparkPos, Quaternion.identity);
            Destroy(sparks, 1f); 
        }
    }

    IEnumerator FlashRoutine()
    {
        if (whiteFlashImage != null)
        {
            whiteFlashImage.color = new Color(1, 1, 1, 1); 
            float alpha = 1f;
            
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * 4f; 
                whiteFlashImage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
        }
    }
}

