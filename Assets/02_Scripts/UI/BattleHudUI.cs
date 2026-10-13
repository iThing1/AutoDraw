using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public class BattleHudUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private HealthBar _playerHealthBar;
    [SerializeField] private HealthBar _bossHealthBar;

    [Header("공격 슬롯")]
    [SerializeField] private AttackSlotUI[] _basicSlots;
    [SerializeField] private AttackSlotUI _skillSlot;

    [Header("데미지 숫자")]
    [SerializeField] private RectTransform _damageTextRoot;
    [SerializeField] private GameObject _damageTextPrefab;
    [SerializeField] private DamageTextSettings _damageText = new();

    [Header("표시")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    private Health _playerHealth;
    private Health _bossHealth;
    private PlayerAttackInput _attackInput;

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

        for (int i = 0; i < _basicSlots.Length; i++)
        {
            bool hasStyle = null != basicStyles && i < basicStyles.Length;

            _basicSlots[i].gameObject.SetActive(hasStyle);

            if (hasStyle)
            {
                _basicSlots[i].Setup(i + 1, basicStyles[i]);
            }
        }

        _skillSlot.Setup(attackInput.SkillKeyNumber, attackInput.Loadout.SkillStyle);

        _attackInput.OnBasicStyleChanged += HandleBasicStyleChanged;
        _attackInput.OnSkillFired += HandleSkillFired;
        _attackInput.SkillGauge.OnChanged += HandleSkillGaugeChanged;

        HandleBasicStyleChanged(_attackInput.SelectedIndex);
        HandleSkillGaugeChanged();
    }

    private void Unbind()
    {
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

    private void SpawnDamageText(DamageInfo info)
    {
        if (null == _damageTextPrefab)
        {
            return;
        }

        Vector2 screenPosition = GameManager.CameraRig.OutputCamera.WorldToScreenPoint(info.HitPoint);

        GameObject instance = GameManager.Pool.Spawn(_damageTextPrefab, _damageTextRoot);

        instance.GetComponent<DamageText>().Play(info.Amount, info.IsCritical, screenPosition, _damageText);
    }
}
