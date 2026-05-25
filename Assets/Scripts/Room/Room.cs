using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [SerializeField] List<EnemyBase> enemies = new();
    [SerializeField] GameObject[] doors;

    bool isCleared;

    void Start()
    {
        EnemyManager.Instance?.RegisterRoom(this, enemies);
        SetDoorsOpen(false);
    }

    public void OnRoomCleared()
    {
        if (isCleared) return;
        isCleared = true;
        SetDoorsOpen(true);
        Debug.Log("Room Cleared!");
    }

    void SetDoorsOpen(bool open)
    {
        foreach (var door in doors)
            if (door != null) door.SetActive(open);
    }
}
