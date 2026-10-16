using System.Collections.Generic;

// 체력바 하나에 보일 값. Health와 BossPhaseMachine(보스만)을 구독해 비율, 기믹 눈금, 경고 상태로 바꿔 둔다.
public class HealthBarViewModel : ViewModelBase
{
    private static readonly float[] NO_MARKERS = new float[0];

    private Health _health;
    private BossPhaseMachine _phases;
    private HealthBarChange _lastChange;
    private bool _isAlert;

    public float Ratio { get { return _health.Ratio; } }
    // 비율이 마지막에 어떻게 바뀌었는지. Ratio 알림을 처리할 때만 의미가 있다.
    public HealthBarChange LastChange { get { return _lastChange; } }
    public IReadOnlyList<float> Markers { get { return null != _phases ? _phases.GimmickRatios : NO_MARKERS; } }
    // 기믹 중이라 체력이 줄지 않는 동안 true.
    public bool IsAlert { get { return _isAlert; } }

    // 페이즈가 없는 유닛(플레이어)은 phases에 null을 넘긴다.
    public HealthBarViewModel(Health health, BossPhaseMachine phases)
    {
        _health = health;
        _phases = phases;

        _health.OnDamaged += HandleDamaged;
        _health.OnHealed += HandleHealed;
        _health.OnRefilled += HandleRefilled;

        if (null != _phases)
        {
            _phases.OnPhaseChanged += HandlePhaseChanged;
            _phases.OnGimmickActiveChanged += HandleGimmickActiveChanged;
        }
    }

    public override void UnBind()
    {
        if (null != _health)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnHealed -= HandleHealed;
            _health.OnRefilled -= HandleRefilled;
            _health = null;
        }

        if (null != _phases)
        {
            _phases.OnPhaseChanged -= HandlePhaseChanged;
            _phases.OnGimmickActiveChanged -= HandleGimmickActiveChanged;
            _phases = null;
        }
    }

    private void HandleDamaged(DamageInfo damage)
    {
        ChangeRatio(HealthBarChange.Damaged);
    }

    private void HandleHealed(int amount)
    {
        ChangeRatio(HealthBarChange.Healed);
    }

    private void HandleRefilled()
    {
        ChangeRatio(HealthBarChange.Refilled);
    }

    private void ChangeRatio(HealthBarChange change)
    {
        _lastChange = change;
        NotifyPropertyChanged(nameof(Ratio));
    }

    private void HandlePhaseChanged()
    {
        NotifyPropertyChanged(nameof(Markers));
    }

    private void HandleGimmickActiveChanged(bool isActive)
    {
        if (_isAlert == isActive)
        {
            return;
        }

        _isAlert = isActive;
        NotifyPropertyChanged(nameof(IsAlert));
    }
}
