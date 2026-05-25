using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    List<EnemyBase> activeEnemies = new();
    Room currentRoom;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterRoom(Room room, List<EnemyBase> enemies)
    {
        currentRoom = room;
        activeEnemies = new List<EnemyBase>(enemies);
    }

    public void OnEnemyDied(EnemyBase enemy)
    {
        activeEnemies.Remove(enemy);
        if (activeEnemies.Count == 0)
            currentRoom?.OnRoomCleared();
    }
}
