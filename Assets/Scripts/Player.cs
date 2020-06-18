using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public CharacterController characterController;
    public Transform shootOrigin;

    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;
    public float throwForce = 600f;
    public float health;
    public float maxHealth = 100;
    public int itemAmount;
    public int maxItemAmount = 3;

    private bool[] _inputs;
    private float _yVelocity = 0;

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    public void Initialize(int id, string userName)
    {
        Id = id;
        UserName = userName;
        _inputs = new bool[5];

        health = maxHealth;
    }

    public void FixedUpdate()
    {
        if (health <= 0)
        {
            return;
        }

        var inputDirection = Vector2.zero;
        if (_inputs[0])
        {
            inputDirection.y += 1;
        }

        if (_inputs[1])
        {
            inputDirection.y -= 1;
        }

        if (_inputs[2])
        {
            inputDirection.x -= 1;
        }

        if (_inputs[3])
        {
            inputDirection.x += 1;
        }

        Move(inputDirection);
    }

    private void Move(Vector2 inputDirection)
    {
        var transform1 = transform;
        var moveDirection = transform1.right * inputDirection.x + transform1.forward * inputDirection.y;
        moveDirection *= moveSpeed;

        if (characterController.isGrounded)
        {
            _yVelocity = 0;
            if (_inputs[4])
            {
                _yVelocity = jumpSpeed;
            }
        }

        _yVelocity += gravity;
        moveDirection.y = _yVelocity;

        characterController.Move(moveDirection);
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    public void SetInput(bool[] inputs, Quaternion rotation)
    {
        _inputs = inputs;
        transform.rotation = rotation;
    }

    public void Shoot(Vector3 viewDirection)
    {
        if (health <= 0)
        {
            return;
        }

        if (Physics.Raycast(shootOrigin.position, viewDirection, out RaycastHit hit, 25f))
        {
            var colliding = hit.collider;
            if (colliding.CompareTag("Player"))
            {
                colliding.GetComponent<Player>().TakeDamage(25);
            }
            else if (colliding.CompareTag("Enemy"))
            {
                colliding.GetComponent<Enemy>().TakeDamage(25);
            }
        }
    }

    public void ThrowItem(Vector3 viewDirection)
    {
        if (health <= 0)
        {
            return;
        }

        if (itemAmount > 0)
        {
            --itemAmount;
            NetworkManager.Instance.InstantiateProjectile(shootOrigin).Initialize(viewDirection, throwForce, Id);
        }
    }

    public void TakeDamage(float damage)
    {
        if (health <= 0)
        {
            return;
        }

        health -= damage;
        if (health <= 0)
        {
            health = 0;
            characterController.enabled = false;
            transform.position = new Vector3(0, 25, 0);
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5);
        health = maxHealth;
        characterController.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount)
        {
            return false;
        }

        itemAmount++;
        return true;
    }
}