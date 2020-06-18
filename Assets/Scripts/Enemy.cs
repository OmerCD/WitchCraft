using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public static int MaxEnemies = 10;
    public static Dictionary<int, Enemy> Enemies = new Dictionary<int, Enemy>();
    private static int _nextEnemyId = 1;

    public int Id { get; private set; }

    public EnemyState state;
    public Player target;
    public CharacterController controller;
    public Transform shootOrigin;
    public float gravity = -9.81f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 8f;
    public float health;
    public float maxHealth = 100f;
    public float detectionRange = 30f;
    public float shootRange = 15f;
    public float shootAccuracy = 0.1f;
    public float patrolDuration = 3f;
    public float idleDuration = 1f;

    private bool _isPatrolRoutineRunning;
    private float _yVelocity;


    private void Start()
    {
        Id = ++_nextEnemyId;
        Enemies.Add(Id, this);
        
        ServerSend.SpawnEnemy(this);
        health = maxHealth;
        state = EnemyState.Patrol;
        gravity *= Time.deltaTime * Time.deltaTime;
        patrolSpeed *= Time.fixedDeltaTime;
        chaseSpeed *= Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case EnemyState.Idle:
                LookForPlayer();
                break;
            case EnemyState.Patrol:
                if (!LookForPlayer())
                {
                    Patrol();
                }

                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool LookForPlayer()
    {
        foreach (var client in Server.Clients.Values)
        {
            if (client.Player != null)
            {
                var enemyToPlayer = client.Player.transform.position - transform.position;
                if (enemyToPlayer.magnitude <= detectionRange)
                {
                    if (Physics.Raycast(shootOrigin.position, enemyToPlayer, out RaycastHit hit, detectionRange))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            target = hit.collider.GetComponent<Player>();
                            if (_isPatrolRoutineRunning)
                            {
                                _isPatrolRoutineRunning = false;
                                StopCoroutine(StartPatrol());
                            }

                            state = EnemyState.Chase;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private void Patrol()
    {
        if (!_isPatrolRoutineRunning)
        {
            StartCoroutine(StartPatrol());
        }

        Move(transform.forward, patrolSpeed);
    }

    private IEnumerator StartPatrol()
    {
        _isPatrolRoutineRunning = true;
        var randomPatrolDirection = Random.insideUnitCircle.normalized;
        transform.forward = new Vector3(randomPatrolDirection.x, 0f, randomPatrolDirection.y);
        yield return new WaitForSeconds(patrolDuration);
        state = EnemyState.Idle;
        yield return new WaitForSeconds(idleDuration);
        state = EnemyState.Patrol;
        _isPatrolRoutineRunning = false;
    }

    private void Chase()
    {
        if (CanSeeTarget())
        {
            var enemyToPlayer = target.transform.position - transform.position;
            if (enemyToPlayer.magnitude <= shootRange)
            {
                state = EnemyState.Attack;
            }
            else
            {
                Move(enemyToPlayer, chaseSpeed);
            }
        }
        else
        {
            target = null;
            state = EnemyState.Patrol;
        }
    }

    private void Attack()
    {
        if (CanSeeTarget())
        {
            var transform1 = transform;
            var enemyToPlayer = target.transform.position - transform1.position;
            transform1.forward = new Vector3(enemyToPlayer.x, 0, enemyToPlayer.z);
            if (enemyToPlayer.magnitude <= shootRange)
            {
                Shoot(enemyToPlayer);
            }
            else
            {
                Move(enemyToPlayer, chaseSpeed);
            }
        }
        else
        {
            target = null;
            state = EnemyState.Patrol;
        }
    }

    private void Move(Vector3 direction, float speed)
    {
        direction.y = 0f;
        transform.forward = direction;
        var movement = transform.forward * speed;
        if (controller.isGrounded)
        {
            _yVelocity = 0f;
        }

        _yVelocity += gravity;
        movement.y = _yVelocity;

        controller.Move(movement);
        
        ServerSend.EnemyPosition(this);
    }

    private void Shoot(Vector3 shootDirection)
    {
        if (Physics.Raycast(shootOrigin.position, shootDirection, out RaycastHit hit, shootRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (Random.value <= shootAccuracy)
                {
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    hit.collider.GetComponent<Player>().TakeDamage(50f);
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            health = 0f;
            Enemies.Remove(Id);
            Destroy(gameObject);
        }
        ServerSend.EnemyHealth(this);
    }

    private bool CanSeeTarget()
    {
        if (!target)
        {
            return false;
        }

        return Physics.Raycast(shootOrigin.position, target.transform.position - transform.position, out RaycastHit hit,
                   detectionRange) && hit.collider.CompareTag("Player");
    }
}

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack
}