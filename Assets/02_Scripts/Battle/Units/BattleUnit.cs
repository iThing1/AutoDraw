using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(AutoAttacker), typeof(UnitView))]
public class BattleUnit : MonoBehaviour
{
    // 막는 동안을 using 블록으로 묶는다. 블록을 벗어나면 정상 종료·예외·취소 모두 막기가 풀린다.
    public readonly struct GuardScope : IDisposable
    {
        private readonly BattleUnit _unit;

        internal GuardScope(BattleUnit unit)
        {
            _unit = unit;
        }

        public void Dispose()
        {
            if (null == _unit)
            {
                return;
            }

            _unit.EndGuard();
        }
    }

    public readonly struct DodgeScope : IDisposable
    {
        private readonly BattleUnit _unit;

        internal DodgeScope(BattleUnit unit)
        {
            _unit = unit;
        }

        public void Dispose()
        {
            if (null == _unit)
            {
                return;
            }

            _unit.EndDodge();
        }
    }

    [SerializeField] private BattleUnitData _data;
    [SerializeField] private Transform _body;
    // Body 아래 그림만 담은 자식. 애니메이션은 이 자식만 움직이고, 판정·넉백·조준은 Body가 맡는다.
    [SerializeField] private SpriteRenderer _bodyRenderer;

    public event Action<UnitState> OnStateChanged;
    // 막은 자리를 넘겨, 막은 쪽이 방패 연출과 막기 문구를 띄우게 한다.
    public event Action<Vector3> OnBlocked;
    public event Action<Vector3> OnDodged;

    private Health _health;
    private AutoAttacker _attacker;
    private UnitView _view;
    private UnitMover _mover;
    private IUnitStats _stats;

    private UnitState _state = UnitState.Idle;
    // 전투 밖에서 쓴 스킬이면 Idle로 돌아간다.
    private UnitState _stateBeforeAction;

    private int _guardCount;
    private int _dodgeCount;
    private float _guardRadius;
    // Action이 끝나기를 기다리는 쪽을 깨우는 신호. 누가 기다릴 때만 만든다.
    private UniTaskCompletionSource _actionEndSource;

    // 키로 고른 공격이 데이터 에셋을 고치지 않도록, 실행 중에만 쓰는 덮어쓰기 값을 따로 둔다.
    private AttackStyle _attackStyleOverride;
    private WeaponModifier _weaponModifier = WeaponModifier.NONE;

    public BattleUnitData Data { get { return _data; } }
    public IUnitStats Stats { get { return _stats; } }
    public Health Health { get { return _health; } }
    public AutoAttacker Attacker { get { return _attacker; } }
    public UnitView View { get { return _view; } }
    public UnitState State { get { return _state; } }
    public bool IsGuarding { get { return 0 < _guardCount; } }
    public bool IsDodging { get { return 0 < _dodgeCount; } }
    public float GuardRadius { get { return _guardRadius; } }

    // 전투 중 움직이지 않는 유닛은 null이다.
    public UnitMover Mover { get { return _mover; } }

    // 넉백·플래시·투사체 조준의 기준점. 따로 지정하지 않으면 자기 자신을 쓴다.
    public Transform Body { get { return null != _body ? _body : transform; } }
    public SpriteRenderer BodyRenderer { get { return _bodyRenderer; } }

    public AttackStyle AttackStyle { get { return null != _attackStyleOverride ? _attackStyleOverride : GetDataAttackStyle(); } }
    public WeaponModifier WeaponModifier { get { return _weaponModifier; } }

    // 렌더러의 bounds는 꺼진 오브젝트나 프리팹에서 0이 되므로 스프라이트 크기에 스케일을 곱해 읽는다. 애니메이션이 늘이는 그림 자식의 크기는 빼, 판정 범위가 출렁이지 않게 한다.
    public Vector2 GetBodyExtents()
    {
        if (null == _bodyRenderer || null == _bodyRenderer.sprite)
        {
            return Vector2.zero;
        }

        return Vector2.Scale(_bodyRenderer.sprite.bounds.extents, Body.lossyScale);
    }

    // 판정에 쓰는 몸 반지름. 몸 그림을 원으로 보고 가로 절반을 쓴다.
    public float GetBodyRadius()
    {
        return GetBodyExtents().x;
    }

    private void Awake()
    {
        _health = GetComponent<Health>();
        _attacker = GetComponent<AutoAttacker>();
        _view = GetComponent<UnitView>();
        TryGetComponent(out _mover);

        _health.OnDied += HandleDied;

        if (null == _bodyRenderer)
        {
            Debug.LogError($"전투 유닛 : {name}에 몸 그림(Visual)이 지정되지 않았습니다. 몸 크기 판정이 0이 됩니다.");
        }

        if (null == _data)
        {
            Debug.LogError($"전투 유닛 : {name}에 BattleUnitData가 지정되지 않았습니다.");
            return;
        }

        // 플레이어처럼 데이터에 스탯이 없으면 만든 쪽이 SetStats로 넘긴다.
        if (null != _data.Stats)
        {
            SetStats(_data.Stats);
        }
    }

    private void OnDestroy()
    {
        if (null != _health)
        {
            _health.OnDied -= HandleDied;
        }
    }

    // 체력도 이 스탯으로 다시 채운다.
    public void SetStats(IUnitStats stats)
    {
        _stats = stats;
        _health.Init(stats.MaxHp);
    }

    public void ChangeState(UnitState nextState)
    {
        if (UnitState.Dead == _state || nextState == _state)
        {
            return;
        }

        UnitState previousState = _state;
        _state = nextState;

        OnStateChanged?.Invoke(_state);

        if (UnitState.Action == previousState)
        {
            NotifyActionEnded();
        }
    }

    public bool TryBeginAction()
    {
        if (UnitState.Action == _state || UnitState.Dead == _state)
        {
            return false;
        }

        _stateBeforeAction = _state;
        ChangeState(UnitState.Action);

        return true;
    }

    public void EndAction()
    {
        if (UnitState.Action != _state)
        {
            return;
        }

        ChangeState(_stateBeforeAction);
    }

    // 다른 동작(패턴·막기)이 끝날 때까지 기다린다. Action이 아니면 바로 끝난다.
    public async UniTask WaitUntilActionEndsAsync(CancellationToken token)
    {
        while (UnitState.Action == _state)
        {
            if (null == _actionEndSource)
            {
                _actionEndSource = new UniTaskCompletionSource();
            }

            await _actionEndSource.Task.AttachExternalCancellation(token);

            // 상태를 바꾸는 호출 안에서 바로 다음 동작이 시작되지 않게 한 프레임 뒤에 이어 간다.
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    public GuardScope BeginGuard(float radius)
    {
        _guardCount++;
        _guardRadius = radius;

        return new GuardScope(this);
    }

    // 피하는 동안에는 공격이 몸을 지나간다. 막을 수 없는 공격(기믹 실패)은 피할 수 없다.
    public DodgeScope BeginDodge()
    {
        _dodgeCount++;

        return new DodgeScope(this);
    }

    public void NotifyBlocked(Vector3 hitPoint)
    {
        OnBlocked?.Invoke(hitPoint);
    }

    public void NotifyDodged(Vector3 point)
    {
        OnDodged?.Invoke(point);
    }

    public void OverrideAttackStyle(AttackStyle attackStyle)
    {
        OverrideAttackStyle(attackStyle, WeaponModifier.NONE);
    }

    public void OverrideAttackStyle(AttackStyle attackStyle, WeaponModifier modifier)
    {
        _attackStyleOverride = attackStyle;
        _weaponModifier = modifier;
    }

    private void EndGuard()
    {
        _guardCount = Mathf.Max(0, _guardCount - 1);
    }

    private void EndDodge()
    {
        _dodgeCount = Mathf.Max(0, _dodgeCount - 1);
    }

    private void NotifyActionEnded()
    {
        if (null == _actionEndSource)
        {
            return;
        }

        UniTaskCompletionSource source = _actionEndSource;
        _actionEndSource = null;
        source.TrySetResult();
    }

    private void HandleDied()
    {
        ChangeState(UnitState.Dead);
    }

    // 데이터가 비어 있으면 Awake에서 이미 로그를 남겼으므로, 여기서는 null을 돌려 공격 쪽의 안내 로그로 이어지게 한다.
    private AttackStyle GetDataAttackStyle()
    {
        return null != _data ? _data.AttackStyle : null;
    }
}
