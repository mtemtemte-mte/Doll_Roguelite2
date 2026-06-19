using UnityEngine;

public class TutorialEnemy : EnemyBase
{
    protected override void Awake()
    {
        maxHp = 1;
        base.Awake();
    }
}
