using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public static Dictionary<int, Projectile> Projectiles = new Dictionary<int, Projectile>();
    private static int _nextProjectileId;

    public int id;
    public Vector3 initialForce;
    public float explosionRadius = 1.5f;
    public float explosionDamage = 75f;

    private Rigidbody _rigidBody;
    private int _thrownByPlayer;
    
    private void Start()
    {
        id = _nextProjectileId++;
        Projectiles.Add(id, this);
        
        ServerSend.SpawnProjectile(this, _thrownByPlayer);
        
        _rigidBody = GetComponent<Rigidbody>();
        _rigidBody.AddForce(initialForce);
        StartCoroutine(ExplodeAfterTime());
    }

    private void FixedUpdate()
    {
        ServerSend.ProjectilePosition(this);
    }

    private void OnCollisionEnter(Collision other)
    {
        Explode();
    }

    public void Initialize(Vector3 initialMovementDirection, float initialForceStrength, int thrownByPlayer)
    {
        initialForce = initialMovementDirection * initialForceStrength;
        _thrownByPlayer = thrownByPlayer;
    }

    private void Explode()
    {
        ServerSend.ProjectileExploded(this);
        var colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var colliding in colliders)
        {
            if (colliding.CompareTag("Player"))
            {
                colliding.GetComponent<Player>().TakeDamage(explosionDamage);
            }
            else if (colliding.CompareTag("Enemy"))
            {
                colliding.GetComponent<Enemy>().TakeDamage(explosionDamage);
            }
        }

        Projectiles.Remove(id);
        Destroy(gameObject);
    }

    private IEnumerator ExplodeAfterTime()
    {
        yield return new WaitForSeconds(10);
        Explode();
    }
}