using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 하단 HUD의 칸 하나. 무기 칸은 선택 표시, 스킬·막기·소모품은 게이지 채움을 보여 준다.
public class ActionSlotView : ViewBase<ActionSlotViewModel>
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _keyText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _selectedFrame;
    [SerializeField] private RectTransform _gaugeFill;

    [Header("연출")]
    [SerializeField, Min(0f)] private float _selectPunch = 0.15f;
    [SerializeField, Min(0f)] private float _punchDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _dimmedAlpha = 0.35f;

    private RectTransform _rect;
    private bool _wasReady;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    protected override void OnDestroy()
    {
        _rect.DOKill();

        base.OnDestroy();
    }

    protected override void OnBind()
    {
        ViewModel.OnUsed += Punch;
    }

    protected override void OnUnbind()
    {
        ViewModel.OnUsed -= Punch;
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(ActionSlotViewModel.IsSelected):
                DrawSelected();
                break;

            case nameof(ActionSlotViewModel.DisplayName):
                _nameText.text = ViewModel.DisplayName;
                break;

            case nameof(ActionSlotViewModel.GaugeRatio):
                DrawGauge();
                break;

            default:
                RefreshAll();
                break;
        }
    }

    protected override void RefreshAll()
    {
        _keyText.text = ViewModel.KeyLabel;
        _nameText.text = ViewModel.DisplayName;
        _icon.color = ViewModel.IconColor;
        _wasReady = false;

        DrawSelected();
        DrawGauge();
    }

    private void DrawSelected()
    {
        bool isSelected = ViewModel.IsSelected;

        if (null != _selectedFrame)
        {
            _selectedFrame.SetActive(isSelected);
        }

        if (isSelected)
        {
            Punch();
        }
    }

    // 게이지가 차는 동안은 아이콘을 흐리게 두고, 가득 차는 순간 밝아지며 튀어 올라 사용할 수 있음을 알린다.
    private void DrawGauge()
    {
        if (false == ViewModel.UsesGauge)
        {
            return;
        }

        bool isReady = ViewModel.IsReady;

        if (null != _gaugeFill)
        {
            _gaugeFill.anchorMax = new Vector2(1f, ViewModel.GaugeRatio);
        }

        Color color = _icon.color;
        color.a = isReady ? 1f : _dimmedAlpha;
        _icon.color = color;

        if (isReady && false == _wasReady)
        {
            Punch();
        }

        _wasReady = isReady;
    }

    private void Punch()
    {
        _rect.DOComplete();
        _rect.DOPunchScale(Vector3.one * _selectPunch, _punchDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY).SetUpdate(true);
    }
}
