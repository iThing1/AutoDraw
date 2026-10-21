using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 HUD 전체에 보일 값. 체력바·하단 칸의 ViewModel을 만들어 품고, 남은 페이즈 수와 떠오르는 문구를 맡는다.
// 전투를 시작하는 쪽이 플레이어와 보스를 넘겨 만들고, HUD를 닫을 때 UnBind로 하위 ViewModel까지 함께 푼다.
public class BattleHudViewModel : ViewModelBase
{
    private readonly HealthBarViewModel _playerHealth;
    private readonly HealthBarViewModel _bossHealth;
    private readonly List<ActionSlotViewModel> _basicSlots = new();
    private readonly ActionSlotViewModel _skillSlot;
    private readonly ActionSlotViewModel _guardSlot;
    private readonly ActionSlotViewModel _itemSlot;

    private BattleUnit _player;
    private BattleUnit _boss;
    private GuardState _guard;
    private BossPhaseMachine _bossPhases;

    // 맞을 때마다 피해 숫자를 띄운다.
    public event Action<DamageInfo> OnDamageText;
    // 막기·게이지 부족·기믹 결과 문구를 월드 위치에 띄운다.
    public event Action<FloatingTextKind, Vector3> OnFloatingText;
    // 회복할 때마다 실제로 찬 양을 띄운다.
    public event Action<int, Vector3> OnHealText;

    public HealthBarViewModel PlayerHealth { get { return _playerHealth; } }
    public HealthBarViewModel BossHealth { get { return _bossHealth; } }
    public IReadOnlyList<ActionSlotViewModel> BasicSlots { get { return _basicSlots; } }
    // 해당 컴포넌트가 없는 플레이어면 null이다.
    public ActionSlotViewModel SkillSlot { get { return _skillSlot; } }
    public ActionSlotViewModel GuardSlot { get { return _guardSlot; } }
    public ActionSlotViewModel ItemSlot { get { return _itemSlot; } }

    public int RemainingPhaseCount { get { return null != _bossPhases ? _bossPhases.RemainingPhaseCount : 1; } }
    // 페이즈가 하나뿐인 보스나 마지막 페이즈에서는 숨긴다.
    public bool ShowsPhaseCount { get { return 1 < RemainingPhaseCount; } }

    public BattleHudViewModel(BattleUnit player, BattleUnit boss)
    {
        _player = player;
        _boss = boss;
        boss.TryGetComponent(out _bossPhases);

        _playerHealth = new HealthBarViewModel(player.Health, null);
        _bossHealth = new HealthBarViewModel(boss.Health, _bossPhases);

        if (player.TryGetComponent(out PlayerAttackInput attackInput))
        {
            for (int i = 0; i < attackInput.SlotCount; i++)
            {
                _basicSlots.Add(new BasicActionSlotViewModel(attackInput, i));
            }

            _skillSlot = new SkillSlotViewModel(attackInput);
        }

        if (player.TryGetComponent(out PlayerGuard guard) && null != guard.State)
        {
            _guard = guard.State;
            _guardSlot = new GuardSlotViewModel(guard.KeyLabel, _guard);
            _guard.OnRejected += HandleGuardRejected;
        }

        if (player.TryGetComponent(out PlayerConsumable consumable) && null != consumable.State)
        {
            _itemSlot = new ConsumableSlotViewModel(consumable.KeyLabel, consumable.State, consumable.IconColor);
        }

        _player.Health.OnDamaged += HandleDamaged;
        _player.Health.OnHealed += HandleHealed;
        _boss.Health.OnDamaged += HandleDamaged;
        _player.OnBlocked += HandleBlocked;

        if (null != _bossPhases)
        {
            _bossPhases.OnPhaseChanged += HandlePhaseChanged;
            _bossPhases.OnGimmickFinished += HandleGimmickFinished;
        }
    }

    public override void UnBind()
    {
        _playerHealth.UnBind();
        _bossHealth.UnBind();

        for (int i = 0; i < _basicSlots.Count; i++)
        {
            _basicSlots[i].UnBind();
        }

        if (null != _skillSlot)
        {
            _skillSlot.UnBind();
        }

        if (null != _guardSlot)
        {
            _guardSlot.UnBind();
        }

        if (null != _itemSlot)
        {
            _itemSlot.UnBind();
        }

        if (null != _guard)
        {
            _guard.OnRejected -= HandleGuardRejected;
            _guard = null;
        }

        if (null != _player)
        {
            _player.Health.OnDamaged -= HandleDamaged;
            _player.Health.OnHealed -= HandleHealed;
            _player.OnBlocked -= HandleBlocked;
            _player = null;
        }

        if (null != _boss)
        {
            _boss.Health.OnDamaged -= HandleDamaged;
            _boss = null;
        }

        if (null != _bossPhases)
        {
            _bossPhases.OnPhaseChanged -= HandlePhaseChanged;
            _bossPhases.OnGimmickFinished -= HandleGimmickFinished;
            _bossPhases = null;
        }
    }

    private void HandleDamaged(DamageInfo damage)
    {
        OnDamageText?.Invoke(damage);
    }

    private void HandleHealed(int amount)
    {
        OnHealText?.Invoke(amount, _player.Body.position);
    }

    private void HandleBlocked(Vector3 hitPoint)
    {
        OnFloatingText?.Invoke(FloatingTextKind.Guard, hitPoint);
    }

    // 쿨타임은 방패가 회색으로 비치는 것으로 충분해, 게이지가 모자랄 때만 알린다.
    private void HandleGuardRejected(GuardStartResult reason)
    {
        if (GuardStartResult.OutOfGauge == reason)
        {
            OnFloatingText?.Invoke(FloatingTextKind.NoGauge, _player.Body.position);
        }
    }

    private void HandleGimmickFinished(bool isSucceeded)
    {
        OnFloatingText?.Invoke(isSucceeded ? FloatingTextKind.GimmickClear : FloatingTextKind.GimmickFail, _boss.Body.position);
    }

    private void HandlePhaseChanged()
    {
        NotifyPropertyChanged(nameof(RemainingPhaseCount));
    }
}
