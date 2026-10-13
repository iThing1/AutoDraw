using UnityEngine;

[RequireComponent(typeof(BattleUnit), typeof(Rigidbody2D))]
public class PlayerAutoMover : MonoBehaviour
{
    [SerializeField] private Vector2 _direction = Vector2.right;

    private BattleUnit _unit;
    private Rigidbody2D _rigidbody;
    private bool _isMoving;

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (false == _isMoving)
        {
            return;
        }

        Vector2 step = _direction.normalized * _unit.Data.MoveSpeed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(_rigidbody.position + step);
    }

    public void StartMoving()
    {
        if (null == _unit.Data)
        {
            Debug.LogError($"자동 이동 : {name}에 BattleUnitData가 지정되지 않았습니다.");
            return;
        }

        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
    }
}
