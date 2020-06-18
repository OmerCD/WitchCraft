using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
   public static Dictionary<int, ItemSpawner> Spawners = new Dictionary<int, ItemSpawner>();
   private static int _nextSpawnerId = 1;

   public int spawnerId;
   public bool hasItem;

   private void Start()
   {
      hasItem = false;
      spawnerId = _nextSpawnerId++;
      Spawners.Add(spawnerId,this);

      StartCoroutine(SpawnItem());
   }

   private void OnTriggerEnter(Collider other)
   {
      if ( hasItem && other.CompareTag("Player"))
      {
         var player = other.GetComponent<Player>();
         if (player.AttemptPickupItem())
         {
            ItemPickup(player.Id);
         }
      }
   }

   private IEnumerator SpawnItem()
   {
      yield return new WaitForSeconds(10);
      hasItem = true;
      ServerSend.ItemSpawned(spawnerId);
   }

   private void ItemPickup(int byPlayer)
   {
      hasItem = false;
      ServerSend.ItemPickedUp(spawnerId, byPlayer);
      StartCoroutine(SpawnItem());
   }
}
