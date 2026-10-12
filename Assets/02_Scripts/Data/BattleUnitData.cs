using UnityEngine;

[CreateAssetMenu(fileName = "BattleUnitData", menuName = "Scriptable Objects/BattleUnitData")]
public class BattleUnitData : ScriptableObject
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 3f;

    public float MoveSpeed { get { return _moveSpeed; } }
}
