using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class ServerSend
    {
        #region Packets

        public static void Welcome(int toClient, string message)
        {
            using (var packet = new Packet((int) ServerPackets.Welcome))
            {
                packet.Write(message);
                packet.Write(toClient);

                SendTcpData(toClient, packet);
            }
        }
        
        public static void SpawnPlayer(in int id, Player clientPlayer)
        {
            using (var packet = new Packet((int) ServerPackets.SpawnPlayer))
            {
                packet.Write(clientPlayer.Id);
                packet.Write(clientPlayer.UserName);
                packet.Write(clientPlayer.transform.position);
                packet.Write(clientPlayer.transform.rotation);

                SendTcpData(id, packet);
            }
        }
        #endregion

        #region UDP

        private static void SendUdpData(in int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Udp.SendData(packet);
        }

        private static void SendUdpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                Server.Clients[i].Udp.SendData(packet);
            }
        }

        private static void SendUdpDataToAllWithExcept(Packet packet, params int[] exceptions)
        {
            SendUdpDataToAllWithExcept(packet, exceptions as IEnumerable<int>);
        }
        private static void SendUdpDataToAllWithExcept(Packet packet, IEnumerable<int> exceptions)
        {
            packet.WriteLength();
            var hashedExceptions = exceptions as HashSet<int> ?? new HashSet<int>(exceptions);
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                if (!hashedExceptions.Contains(i))
                {
                    Server.Clients[i].Udp.SendData(packet);
                }
            }
        }

        #endregion

        #region TCP

        private static void SendTcpData(in int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }

        private static void SendTcpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
        private static void SendTcpDataToAllWithExcept(Packet packet, params int[] exceptions)
        {
            SendTcpDataToAllWithExcept(packet, exceptions as IEnumerable<int>);
        }
        private static void SendTcpDataToAllWithExcept(Packet packet, IEnumerable<int> exceptions)
        {
            packet.WriteLength();
            var hashedExceptions = exceptions as HashSet<int> ?? new HashSet<int>(exceptions);
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                if (!hashedExceptions.Contains(i))
                {
                    Server.Clients[i].Tcp.SendData(packet);
                }
            }
        }

        #endregion

        public static void PlayerPosition(Player player)
        {
            using (var packet = new Packet((int) ServerPackets.PlayerPosition))
            {
                packet.Write(player.Id);
                packet.Write(player.transform.position);
                SendUdpDataToAll(packet);
            }
        }
        public static void PlayerRotation(Player player)
        {
            using (var packet = new Packet((int) ServerPackets.PlayerRotation))
            {
                packet.Write(player.Id);
                packet.Write(player.transform.rotation);
                SendUdpDataToAllWithExcept(packet, player.Id);
            }
        }

        public static void PlayerDisconnected(int playerId)
        {
            using (var packet = new Packet((int)ServerPackets.PlayerDisconnected))
            {
                packet.Write(playerId);
                SendTcpDataToAll(packet);
            }
        }

        public static void PlayerHealth(Player player)
        {
            using (var packet = new Packet((int)ServerPackets.PlayerHealth))
            {
                packet.Write(player.Id);
                packet.Write(player.health);
                
                SendTcpDataToAll(packet);
            }
        }
        public static void PlayerRespawned(Player player)
        {
            using (var packet = new Packet((int)ServerPackets.PlayerRespawned))
            {
                packet.Write(player.Id);
                
                SendTcpDataToAll(packet);
            }
        }

        public static void CreateItemSpawner(int toClient, int spawnerId, Vector3 spawnerPosition, bool hasItem)
        {
            using (var packet = new Packet((int)ServerPackets.CreateItemSpawner))
            {
                packet.Write(spawnerId);
                packet.Write(spawnerPosition);
                packet.Write(hasItem);
                
                SendTcpData(toClient, packet);
            }
        }

        public static void ItemSpawned(int spawnerId)
        {
            using (var packet = new Packet((int)ServerPackets.ItemSpawned))
            {
                packet.Write(spawnerId);
                SendTcpDataToAll(packet);
            }
        }
        public static void ItemPickedUp(int spawnerId, int byPlayer)
        {
            using (var packet = new Packet((int)ServerPackets.ItemPickedUp))
            {
                packet.Write(spawnerId);
                packet.Write(byPlayer);
                SendTcpDataToAll(packet);
            }
        }

        public static void SpawnProjectile(Projectile projectile, int thrownByPlayer)
        {
            using (var packet = new Packet(ServerPackets.SpawnProjectile))
            {
                packet.Write(projectile.id);
                packet.Write(projectile.transform.position);
                packet.Write(thrownByPlayer);
                SendTcpDataToAll(packet);
            }
        }
        public static void ProjectilePosition(Projectile projectile)
        {
            using (var packet = new Packet(ServerPackets.ProjectilePosition))
            {
                packet.Write(projectile.id);
                packet.Write(projectile.transform.position);
                SendUdpDataToAll(packet);
            }
        }
        public static void ProjectileExploded(Projectile projectile)
        {
            using (var packet = new Packet(ServerPackets.ProjectileExploded))
            {
                packet.Write(projectile.id);
                packet.Write(projectile.transform.position);
                SendTcpDataToAll(packet);
            }
        }

        public static void SpawnEnemy(Enemy enemy)
        {
            using (var packet = new Packet(ServerPackets.SpawnEnemy))
            {
                SendTcpDataToAll(SpawnEnemyData(enemy,packet));
            }
        }
        public static void SpawnEnemy(int toClient, Enemy enemy)
        {
            using (var packet = new Packet(ServerPackets.SpawnEnemy))
            {
                SendTcpData(toClient, SpawnEnemyData(enemy,packet));
            }
        }
        private static Packet SpawnEnemyData(Enemy enemy, Packet packet)
        {
            packet.Write(enemy.Id);
            packet.Write(enemy.transform.position);
            return packet;
        }

        public static void EnemyPosition(Enemy enemy)
        {
            using (var packet = new Packet(ServerPackets.EnemyPosition))
            {
                packet.Write(enemy.Id);
                packet.Write(enemy.transform.position);
                SendUdpDataToAll(packet);
            }
        }
        public static void EnemyHealth(Enemy enemy)
        {
            using (var packet = new Packet(ServerPackets.EnemyHealth))
            {
                packet.Write(enemy.Id);
                packet.Write(enemy.health);
                SendTcpDataToAll(packet);
            }
        }
    }