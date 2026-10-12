using UnityEngine;

public class BossUnit : MonoBehaviour
{
    [SerializeField] private BattleUnitData _data;

    public BattleUnitData Data { get { return _data; } }
}
