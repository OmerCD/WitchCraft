using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; set; }
    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public GameObject projectilePrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Server.Start(4, 3000);
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity).GetComponent<Player>();
    }

    public void InstantiateEnemy(Vector3 position)
    {
        Instantiate(enemyPrefab, position, Quaternion.identity);
    }

    public Projectile InstantiateProjectile(Transform shootOrigin)
    {
        return Instantiate(projectilePrefab, shootOrigin.position + shootOrigin.forward * 0.7f, Quaternion.identity).GetComponent<Projectile>();
    }
}