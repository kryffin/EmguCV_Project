using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BallBehavior : MonoBehaviour
{
    private Rigidbody2D _rb;
    private float _currentSpeed;
    private int _direction;
    private int[] _scores;

    [Range(0.5f, 10f)]
    public float ballSpeed = 10f;
    public float maxSpeed = 20f;
    public ParticleSystem greenGoal;
    public ParticleSystem blueGoal;
    public TextMeshProUGUI firstPlayerScore;
    public TextMeshProUGUI secondPlayerScore;

    [Header("Ball colors")]
    [Tooltip("Ball color at start speed")]
    public Color startColor;
    [Tooltip("Ball color at max speed")]
    public Color endColor;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _currentSpeed = ballSpeed;
        _scores = new[] {0, 0};

        _direction = Random.Range(0, 2) == 0 ? -1 : 1;
        BallReset();
    }

    // Returns a random velocity, 90° around the direction towards the goal of the player that didn't score last goal
    private Vector2 RandomVelocity()
    {
        float randomAngle = Random.Range(45f * Mathf.Deg2Rad, 135f * Mathf.Deg2Rad) * _direction;
        Vector2 randomDirection = new Vector2(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle)).normalized;

        //avoid setting a direction with y close to 0, it would mean the ball won't ever go diagonally
        if (randomDirection.y < .1f && randomDirection.y > -.1f)
            randomDirection.y += .2f;

        return randomDirection.normalized;
    }

    // Resets the position of the ball and gives it a new random velocity
    private void BallReset()
    {
        _rb.position = Vector2.zero;
        _rb.velocity = RandomVelocity() * ballSpeed;
        _currentSpeed = ballSpeed;
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
            spriteRenderer.color = startColor;
    }


    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.transform.CompareTag("Player")) return;
        int layer = col.transform.gameObject.layer;
        
        if (layer == 6 && _direction == -1 || layer == 7 && _direction == 1)
        {
            if (_currentSpeed < maxSpeed)
                _currentSpeed += 1;
            _direction *= -1;

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
                spriteRenderer.color = Color.Lerp(startColor, endColor, (_currentSpeed - ballSpeed) / (maxSpeed - ballSpeed));
            
            _rb.velocity = _rb.velocity.normalized * _currentSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool player1Scored = collision.gameObject.name == "Goal 2";
        _direction *= -1;

        // Plays the according goal's Particle System
        if (player1Scored)
        {
            _scores[0] += 1;
            firstPlayerScore.text = _scores[0].ToString();
            blueGoal.Play();
        }
        else
        {
            _scores[1] += 1;
            secondPlayerScore.text = _scores[1].ToString();
            greenGoal.Play();
        }
        
        BallReset();
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
