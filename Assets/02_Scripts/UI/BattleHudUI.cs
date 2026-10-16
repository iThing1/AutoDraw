using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class BattleHudUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private HealthBar _playerHealthBar;
    [SerializeField] private HealthBar _bossHealthBar;

    [Header("보스 페이즈")]
    // 남은 페이즈 수. 페이즈가 하나뿐인 보스나 마지막 페이즈에서는 숨긴다.
    [SerializeField] private TMP_Text _bossPhaseText;
    [SerializeField] private string _bossPhaseFormat = "x{0}";

    [Header("공격 슬롯")]
    [SerializeField] private AttackSlotUI[] _basicSlots;
    [SerializeField] private AttackSlotUI _skillSlot;
    [SerializeField] private AttackSlotUI _guardSlot;

    [Header("데미지 숫자")]
    [SerializeField] private RectTransform _damageTextRoot;
    [SerializeField] private GameObject _damageTextPrefab;
    [SerializeField] private DamageTextSettings _damageText = new();

    [Header("표시")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    private Health _playerHealth;
    private Health _bossHealth;
    private PlayerAttackInput _attackInput;
    private BattleUnit _player;
    private PlayerGuard _guard;
    private BossPhaseMachine _bossPhases;
    private BattleUnit _boss;

    private void Awake()
    {
        _canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(BattleUnit player, BattleUnit boss)
    {
        Unbind();

        _playerHealth = player.Health;
        _bossHealth = boss.Health;

        _playerHealthBar.Bind(_playerHealth);
        _bossHealthBar.Bind(_bossHealth);

        _playerHealth.OnDamaged += SpawnDamageText;
        _bossHealth.OnDamaged += SpawnDamageText;

        BindAttackSlots(player.GetComponent<PlayerAttackInput>());
        BindGuard(player);
        BindBossPhases(boss);
    }

    public void Show()
    {
        _canvasGroup.DOKill();
        _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
    }

    public UniTask HideAsync(CancellationToken token)
    {
        _canvasGroup.DOKill();

        return _canvasGroup.DOFade(0f, _fadeDuration)
            .SetUpdate(true)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    private void BindAttackSlots(PlayerAttackInput attackInput)
    {
        if (null == attackInput || null == attackInput.Loadout)
        {
            return;
        }

        _attackInput = attackInput;

        AttackStyle[] basicStyles = attackInput.Loadout.BasicStyles;
        int basicCount = attackInput.BasicStyleCount;

        for (int i = 0; i < _basicSlots.Length; i++)
        {
            bool hasStyle = i < basicCount;

            _basicSlots[i].gameObject.SetActive(hasStyle);

            if (hasStyle)
            {
                _basicSlots[i].Setup(attackInput.GetBasicKeyLabel(i), basicStyles[i]);
            }
        }

        _skillSlot.Setup(attackInput.SkillKeyLabel, attackInput.Loadout.SkillStyle);

        _attackInput.OnBasicStyleChanged += HandleBasicStyleChanged;
        _attackInput.OnSkillFired += HandleSkillFired;
        _attackInput.SkillGauge.OnChanged += HandleSkillGaugeChanged;

        HandleBasicStyleChanged(_attackInput.SelectedIndex);
        HandleSkillGaugeChanged();
    }

    private void BindGuard(BattleUnit player)
    {
        _player = player;
        _player.OnBlocked += SpawnGuardText;

        _guard = player.GetComponent<PlayerGuard>();

        if (null == _guard || null == _guard.Preset || null == _guardSlot)
        {
            return;
        }

        _guardSlot.Setup(_guard.KeyLabel, _guard.Preset.DisplayName, _guard.Preset.IconColor);

        _guard.OnGuardStarted += HandleGuardStarted;
        _guard.OnGuardRejected += HandleGuardRejected;
        _guard.SkillGauge.OnChanged += HandleGuardGaugeChanged;

        HandleGuardGaugeChanged();
    }

    private void BindBossPhases(BattleUnit boss)
    {
        _boss = boss;

        if (false == boss.TryGetComponent(out _bossPhases))
        {
            _bossHealthBar.SetMarkers(Array.Empty<float>());
            SetBossPhaseTextVisible(false);
            return;
        }

        _bossPhases.OnPhaseChanged += HandleBossPhaseChanged;
        _bossPhases.OnGimmickActiveChanged += _bossHealthBar.SetAlert;
        _bossPhases.OnGimmickFinished += HandleGimmickFinished;

        HandleBossPhaseChanged();
    }

    private void Unbind()
    {
        if (null != _bossPhases)
        {
            _bossPhases.OnPhaseChanged -= HandleBossPhaseChanged;
            _bossPhases.OnGimmickActiveChanged -= _bossHealthBar.SetAlert;
            _bossPhases.OnGimmickFinished -= HandleGimmickFinished;
            _bossPhases = null;
        }

        _boss = null;

        if (null != _player)
        {
            _player.OnBlocked -= SpawnGuardText;
            _player = null;
        }

        if (null != _guard)
        {
            _guard.OnGuardStarted -= HandleGuardStarted;
            _guard.OnGuardRejected -= HandleGuardRejected;
            _guard.SkillGauge.OnChanged -= HandleGuardGaugeChanged;
            _guard = null;
        }

        if (null != _playerHealth)
        {
            _playerHealth.OnDamaged -= SpawnDamageText;
            _playerHealth = null;
        }

        if (null != _bossHealth)
        {
            _bossHealth.OnDamaged -= SpawnDamageText;
            _bossHealth = null;
        }

        if (null != _attackInput)
        {
            _attackInput.OnBasicStyleChanged -= HandleBasicStyleChanged;
            _attackInput.OnSkillFired -= HandleSkillFired;
            _attackInput.SkillGauge.OnChanged -= HandleSkillGaugeChanged;
            _attackInput = null;
        }
    }

    private void HandleBossPhaseChanged()
    {
        _bossHealthBar.SetMarkers(_bossPhases.GimmickRatios);

        int remaining = _bossPhases.RemainingPhaseCount;

        SetBossPhaseTextVisible(1 < remaining);

        if (null != _bossPhaseText)
        {
            _bossPhaseText.text = string.Format(_bossPhaseFormat, remaining);
        }
    }

    private void HandleGimmickFinished(bool isSucceeded)
    {
        DamageText damageText = SpawnText();

        if (null == damageText)
        {
            return;
        }

        string text = isSucceeded ? _damageText.GimmickClearText : _damageText.GimmickFailText;
        Color color = isSucceeded ? _damageText.GimmickClearColor : _damageText.GimmickFailColor;

        damageText.PlayText(text, color, ToScreen(_boss.Body.position), _damageText);
    }

    private void SetBossPhaseTextVisible(bool isVisible)
    {
        if (null != _bossPhaseText)
        {
            _bossPhaseText.gameObject.SetActive(isVisible);
        }
    }

    private void HandleBasicStyleChanged(int selectedIndex)
    {
        for (int i = 0; i < _basicSlots.Length; i++)
        {
            _basicSlots[i].SetSelected(i == selectedIndex);
        }
    }

    private void HandleSkillGaugeChanged()
    {
        SkillGauge gauge = _attackInput.SkillGauge;

        _skillSlot.SetGauge(gauge.Ratio, gauge.IsFull);
    }

    private void HandleSkillFired()
    {
        _skillSlot.PlayUsed();
    }

    // 막기 비용만큼 게이지가 차면 칸이 밝아지며 튀어, 막기를 쓸 수 있음을 알린다.
    private void HandleGuardGaugeChanged()
    {
        float cost = _guard.Preset.GaugeCost;
        float ratio = 0f < cost ? Mathf.Clamp01(_guard.SkillGauge.Current / cost) : 1f;

        _guardSlot.SetGauge(ratio, _guard.SkillGauge.CanConsume(cost));
    }

    private void HandleGuardStarted()
    {
        _guardSlot.PlayUsed();
    }

    // 게이지가 모자라 막기가 안 나갔으면 플레이어 머리 위에 알린다. 쿨타임은 방패가 회색으로 비치는 것으로 충분하다.
    private void HandleGuardRejected(bool isOutOfGauge)
    {
        if (false == isOutOfGauge)
        {
            return;
        }

        DamageText damageText = SpawnText();

        if (null != damageText)
        {
            damageText.PlayText(_damageText.NoGaugeText, _damageText.NoGaugeColor, ToScreen(_player.Body.position), _damageText);
        }
    }

    private void SpawnDamageText(DamageInfo damage)
    {
        DamageText damageText = SpawnText();

        if (null != damageText)
        {
            damageText.Play(damage.Amount, damage.IsCritical, ToScreen(damage.HitPoint), _damageText);
        }
    }

    private void SpawnGuardText(Vector3 hitPoint)
    {
        DamageText damageText = SpawnText();

        if (null != damageText)
        {
            damageText.PlayText(_damageText.GuardText, _damageText.GuardColor, ToScreen(hitPoint), _damageText);
        }
    }

    private DamageText SpawnText()
    {
        if (null == _damageTextPrefab)
        {
            return null;
        }

        return GameManager.Pool.Spawn(_damageTextPrefab, _damageTextRoot).GetComponent<DamageText>();
    }

    private Vector2 ToScreen(Vector3 worldPosition)
    {
        return GameManager.CameraRig.OutputCamera.WorldToScreenPoint(worldPosition);
    }
}
