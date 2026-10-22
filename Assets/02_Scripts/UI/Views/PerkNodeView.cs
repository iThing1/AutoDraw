using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특성 트리 화면의 노드 하나. 아이콘 그림이 없으면 트리 색 타일에 이름 첫 글자를 얹는다.
public class PerkNodeView : ViewBase<PerkNodeViewModel>
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _selectedFrame;
    [SerializeField] private TMP_Text _costText;

    [Header("아이콘")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _initial;

    [Header("상태")]
    [SerializeField] private Color _normalColor = new Color(0.15f, 0.15f, 0.17f, 0.92f);
    [SerializeField] private Color _unlockedColor = new Color(0.85f, 0.65f, 0.3f, 1f);
    [SerializeField] private Color _costColor = Color.white;
    [SerializeField] private Color _shortCostColor = new Color(0.9f, 0.35f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float _lockedAlpha = 0.35f;

    private Color _treeColor = Color.white;

    public RectTransform Rect { get { return (RectTransform)transform; } }

    private void Awake()
    {
        _button.onClick.AddListener(HandleClicked);
    }

    // 트리 색은 화면에만 쓰는 값이라 트리 화면 View가 Bind 전에 넣어 준다.
    public void SetTreeColor(Color treeColor)
    {
        _treeColor = treeColor;
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(PerkNodeViewModel.State):
                DrawState();
                break;

            case nameof(PerkNodeViewModel.IsSelected):
                _selectedFrame.SetActive(ViewModel.IsSelected);
                break;
        }
    }

    protected override void RefreshAll()
    {
        DrawIcon();
        DrawState();
        _selectedFrame.SetActive(ViewModel.IsSelected);
        _costText.SetText("{0}", ViewModel.Cost);
    }

    private void DrawIcon()
    {
        Sprite icon = ViewModel.Icon;
        bool hasSprite = null != icon;

        _icon.sprite = icon;
        _icon.color = hasSprite ? Color.white : _treeColor;

        _initial.gameObject.SetActive(false == hasSprite);
        _initial.text = 0 < ViewModel.Label.Length ? ViewModel.Label.Substring(0, 1) : string.Empty;
    }

    // 찍은 노드는 금색, 부모를 덜 찍은 노드는 흐리게, 포인트만 모자란 노드는 비용을 붉게 그린다.
    private void DrawState()
    {
        PerkNodeState state = ViewModel.State;

        _background.color = PerkNodeState.Unlocked == state ? _unlockedColor : _normalColor;
        _canvasGroup.alpha = PerkNodeState.Locked == state ? _lockedAlpha : 1f;
        _costText.gameObject.SetActive(PerkNodeState.Unlocked != state);
        _costText.color = PerkNodeState.Unaffordable == state ? _shortCostColor : _costColor;
    }

    private void HandleClicked()
    {
        if (null != ViewModel)
        {
            ViewModel.Select();
        }
    }
}
