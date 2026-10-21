using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

// 전투 HUD. 하위 View(체력바·하단 칸)에 ViewModel의 하위 ViewModel을 꽂고, 남은 페이즈 수와 떠오르는 문구만 직접 그린다.
public class BattleHudView : ViewBase<BattleHudViewModel>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private HealthBarView _playerHealthBar;
    [SerializeField] private HealthBarView _bossHealthBar;

    [Header("보스 페이즈")]
    // 남은 페이즈 수. 페이즈가 하나뿐인 보스나 마지막 페이즈에서는 숨긴다.
    [SerializeField] private TMP_Text _bossPhaseText;
    [SerializeField] private string _bossPhaseFormat = "x{0}";

    [Header("하단 칸")]
    [SerializeField] private ActionSlotView[] _weaponSlots;
    [SerializeField] private ActionSlotView _skillSlot;
    [SerializeField] private ActionSlotView _guardSlot;
    [SerializeField] private ActionSlotView _itemSlot;

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
            .ToUniTaskKillOnCancel(token);
    }

    protected override void OnBind()
    {
        BattleHudViewModel viewModel = ViewModel;

        _playerHealthBar.Bind(viewModel.PlayerHealth);
        _bossHealthBar.Bind(viewModel.BossHealth);

        BindWeaponSlots(viewModel.WeaponSlots);
        BindSlot(_skillSlot, viewModel.SkillSlot);
        BindSlot(_guardSlot, viewModel.GuardSlot);
        BindSlot(_itemSlot, viewModel.ItemSlot);

        viewModel.OnDamageText += SpawnDamageText;
        viewModel.OnFloatingText += SpawnFloatingText;
        viewModel.OnHealText += SpawnHealText;
    }

    protected override void OnUnbind()
    {
        ViewModel.OnDamageText -= SpawnDamageText;
        ViewModel.OnFloatingText -= SpawnFloatingText;
        ViewModel.OnHealText -= SpawnHealText;

        _playerHealthBar.UnBind();
        _bossHealthBar.UnBind();

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            _weaponSlots[i].UnBind();
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

    // 쓰는 무기 칸 수만큼만 칸을 켠다.
    private void BindWeaponSlots(IReadOnlyList<ActionSlotViewModel> slots)
    {
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            bool hasSlot = i < slots.Count;

            _weaponSlots[i].gameObject.SetActive(hasSlot);

            if (hasSlot)
            {
                _weaponSlots[i].Bind(slots[i]);
            }
        }
    }

    // 쓰지 않는 칸(유틸리티가 막기가 아닐 때의 막기 칸 등)은 숨긴다.
    private void BindSlot(ActionSlotView slot, ActionSlotViewModel viewModel)
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
            _bossPhaseText.SetText(_bossPhaseFormat, ViewModel.RemainingPhaseCount);
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
        SpawnPhrase(phrase.Text, phrase.Color, worldPosition);
    }

    // 회복 문구는 "+{0}"처럼 회복량 자리를 둔다.
    private void SpawnHealText(int amount, Vector3 worldPosition)
    {
        FloatingTextSettings.Phrase phrase = _floatingText.GetPhrase(FloatingTextKind.Heal);

        if (string.IsNullOrEmpty(phrase.Text))
        {
            return;
        }

        FloatingText floatingText = SpawnText();

        if (null != floatingText)
        {
            floatingText.PlayNumber(phrase.Text, amount, phrase.Color, ToScreen(worldPosition), _floatingText);
        }
    }

    // 설정에 없는 종류는 문구가 비어 있어 띄우지 않는다.
    private void SpawnPhrase(string text, Color color, Vector3 worldPosition)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        FloatingText floatingText = SpawnText();

        if (null == floatingText)
        {
            return;
        }

        floatingText.PlayText(text, color, ToScreen(worldPosition), _floatingText);
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
