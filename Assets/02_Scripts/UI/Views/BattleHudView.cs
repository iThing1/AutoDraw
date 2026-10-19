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

    [Header("떠오르는 문구")]
    // 피해 숫자와 GUARD·NO INK·BREAK 같은 짧은 문구를 같은 프리팹으로 띄운다.
    [SerializeField] private RectTransform _floatingTextRoot;
    [SerializeField] private GameObject _floatingTextPrefab;
    [SerializeField] private FloatingTextSettings _floatingText = new();

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

    // 쓰지 않는 칸(유틸리티가 막기가 아닐 때의 막기 칸 등)은 숨긴다.
    private void BindSlot(AttackSlotView slot, AttackSlotViewModel viewModel)
    {
        if (null == slot)
        {
            return;
        }

        slot.gameObject.SetActive(null != viewModel);

        if (null != viewModel)
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
        FloatingText floatingText = SpawnText();

        if (null != floatingText)
        {
            floatingText.Play(damage.Amount, damage.IsCritical, ToScreen(damage.HitPoint), _floatingText);
        }
    }

    private void SpawnFloatingText(FloatingTextKind kind, Vector3 worldPosition)
    {
        FloatingTextSettings.Phrase phrase = _floatingText.GetPhrase(kind);

        // 설정에 없는 종류는 띄우지 않는다.
        if (string.IsNullOrEmpty(phrase.Text))
        {
            return;
        }

        FloatingText floatingText = SpawnText();

        if (null == floatingText)
        {
            return;
        }

        floatingText.PlayText(phrase.Text, phrase.Color, ToScreen(worldPosition), _floatingText);
    }

    private FloatingText SpawnText()
    {
        if (null == _floatingTextPrefab)
        {
            return null;
        }

        return GameManager.Pool.Spawn(_floatingTextPrefab, _floatingTextRoot).GetComponent<FloatingText>();
    }

    private Vector2 ToScreen(Vector3 worldPosition)
    {
        return GameManager.CameraRig.OutputCamera.WorldToScreenPoint(worldPosition);
    }
}
