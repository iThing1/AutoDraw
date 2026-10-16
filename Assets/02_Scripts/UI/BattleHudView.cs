using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

// 전투 HUD. 하위 View(체력바·공격 칸)에 ViewModel의 하위 ViewModel을 꽂고, 남은 페이즈 수와 떠오르는 문구만 직접 그린다.
public class BattleHudView : ViewBase<BattleHudViewModel>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private HealthBarView _playerHealthBar;
    [SerializeField] private HealthBarView _bossHealthBar;

    [Header("보스 페이즈")]
    // 남은 페이즈 수. 페이즈가 하나뿐인 보스나 마지막 페이즈에서는 숨긴다.
    [SerializeField] private TMP_Text _bossPhaseText;
    [SerializeField] private string _bossPhaseFormat = "x{0}";

    [Header("공격 슬롯")]
    [SerializeField] private AttackSlotView[] _basicSlots;
    [SerializeField] private AttackSlotView _skillSlot;
    [SerializeField] private AttackSlotView _guardSlot;

    [Header("데미지 숫자")]
    [SerializeField] private RectTransform _damageTextRoot;
    [SerializeField] private GameObject _damageTextPrefab;
    [SerializeField] private DamageTextSettings _damageText = new();

    [Header("표시")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    private void Awake()
    {
        _canvasGroup.alpha = 0f;
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

    protected override void OnBind()
    {
        BattleHudViewModel viewModel = ViewModel;

        _playerHealthBar.Bind(viewModel.PlayerHealth);
        _bossHealthBar.Bind(viewModel.BossHealth);

        BindBasicSlots(viewModel.BasicSlots);
        BindSlot(_skillSlot, viewModel.SkillSlot);
        BindSlot(_guardSlot, viewModel.GuardSlot);

        viewModel.OnDamageText += SpawnDamageText;
        viewModel.OnFloatingText += SpawnFloatingText;
    }

    protected override void OnUnbind()
    {
        ViewModel.OnDamageText -= SpawnDamageText;
        ViewModel.OnFloatingText -= SpawnFloatingText;

        _playerHealthBar.UnBind();
        _bossHealthBar.UnBind();

        for (int i = 0; i < _basicSlots.Length; i++)
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
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(BattleHudViewModel.RemainingPhaseCount):
                DrawPhaseCount();
                break;

            default:
                RefreshAll();
                break;
        }
    }

    protected override void RefreshAll()
    {
        DrawPhaseCount();
    }

    // 쓰는 기본 공격 수만큼만 칸을 켠다.
    private void BindBasicSlots(IReadOnlyList<AttackSlotViewModel> slots)
    {
        for (int i = 0; i < _basicSlots.Length; i++)
        {
            bool hasSlot = i < slots.Count;

            _basicSlots[i].gameObject.SetActive(hasSlot);

            if (hasSlot)
            {
                _basicSlots[i].Bind(slots[i]);
            }
        }
    }

    private void BindSlot(AttackSlotView slot, AttackSlotViewModel viewModel)
    {
        if (null != slot && null != viewModel)
        {
            slot.Bind(viewModel);
        }
    }

    private void DrawPhaseCount()
    {
        if (null == _bossPhaseText)
        {
            return;
        }

        bool isVisible = ViewModel.ShowsPhaseCount;

        _bossPhaseText.gameObject.SetActive(isVisible);

        if (isVisible)
        {
            _bossPhaseText.text = string.Format(_bossPhaseFormat, ViewModel.RemainingPhaseCount);
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

    private void SpawnFloatingText(FloatingTextKind kind, Vector3 worldPosition)
    {
        DamageText damageText = SpawnText();

        if (null == damageText)
        {
            return;
        }

        damageText.PlayText(GetFloatingText(kind), GetFloatingColor(kind), ToScreen(worldPosition), _damageText);
    }

    private string GetFloatingText(FloatingTextKind kind)
    {
        switch (kind)
        {
            case FloatingTextKind.Guard:
                return _damageText.GuardText;

            case FloatingTextKind.NoGauge:
                return _damageText.NoGaugeText;

            case FloatingTextKind.GimmickClear:
                return _damageText.GimmickClearText;

            default:
                return _damageText.GimmickFailText;
        }
    }

    private Color GetFloatingColor(FloatingTextKind kind)
    {
        switch (kind)
        {
            case FloatingTextKind.Guard:
                return _damageText.GuardColor;

            case FloatingTextKind.NoGauge:
                return _damageText.NoGaugeColor;

            case FloatingTextKind.GimmickClear:
                return _damageText.GimmickClearColor;

            default:
                return _damageText.GimmickFailColor;
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
