using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAutoMover : MonoBehaviour
{
    [SerializeField] private BattleUnitData _data;
    [SerializeField] private Vector2 _direction = Vector2.right;

    private Rigidbody2D _rigidbody;
    private bool _isMoving;

    public bool IsMoving { get { return _isMoving; } }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (false == _isMoving)
        {
            return;
        }

        Vector2 step = _direction.normalized * _data.MoveSpeed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(_rigidbody.position + step);
    }

    public void StartMoving()
    {
        if (null == _data)
        {
            Debug.LogError("자동 이동 : BattleUnitData가 지정되지 않았습니다.");
            return;
        }

        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
    }
}
