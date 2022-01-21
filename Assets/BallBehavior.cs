using UnityEngine;

public class BallBehavior : MonoBehaviour
{
    private Rigidbody2D _rb;

    [Range(0.5f, 10f)]
    public float BallSpeed = 2f;
    public ParticleSystem redGoal;
    public ParticleSystem blueGoal;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        BallReset(Random.Range(0, 2) == 0);
    }

    // Returns a random velocity, 90° around the direction towards the goal of the player that didn't score last goal
    private Vector2 RandomVelocity(bool player1Scored)
    {
        float randomAngle = Random.Range(45f * Mathf.Deg2Rad, 135f * Mathf.Deg2Rad);
        if (player1Scored) randomAngle = -randomAngle;
        Vector2 randomDirection = new Vector2(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle)).normalized;

        //avoid setting a direction with y close to 0, it would mean the ball won't ever go diagonally
        if (randomDirection.y < .1f && randomDirection.y > -.1f)
            randomDirection.y += .2f;

        return randomDirection.normalized;
    }

    // Resets the position of the ball and gives it a new random velocity
    private void BallReset(bool player1Scored)
    {
        _rb.position = Vector2.zero;
        _rb.velocity = RandomVelocity(player1Scored) * BallSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool player1Scored = collision.gameObject.name == "Goal 2";

        // Plays the according goal's Particle System
        if (player1Scored)
            blueGoal.Play();
        else
            redGoal.Play();

        BallReset(player1Scored);
    }

    private void OnDrawGizmosSelected()
    {
        // Displays the angle the ball will launch depending on who scored last

        Gizmos.color = new Color(1f, 0f, 0f, .5f);
        Gizmos.DrawRay(new Ray(Vector3.zero, new Vector2(Mathf.Sin(45f * Mathf.Deg2Rad), Mathf.Cos(45f * Mathf.Deg2Rad)).normalized));
        Gizmos.DrawRay(new Ray(Vector3.zero, new Vector2(Mathf.Sin(135f * Mathf.Deg2Rad), Mathf.Cos(135f * Mathf.Deg2Rad)).normalized));

        Gizmos.color = new Color(0f, 0f, 1f, .5f);
        Gizmos.DrawRay(new Ray(Vector3.zero, new Vector2(Mathf.Sin(-45f * Mathf.Deg2Rad), Mathf.Cos(-45f * Mathf.Deg2Rad)).normalized));
        Gizmos.DrawRay(new Ray(Vector3.zero, new Vector2(Mathf.Sin(-135f * Mathf.Deg2Rad), Mathf.Cos(-135f * Mathf.Deg2Rad)).normalized));

        // Displays the ball's direction

        if (_rb != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(new Ray(_rb.position, _rb.velocity.normalized * 2f));
        }
    }
}
