using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 장비 화면의 장비 버튼 하나. 아이콘 그림이 없으면 색 타일에 이름 첫 글자를 얹는다.
public class LoadoutItemView : ViewBase<LoadoutItemViewModel>
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _label;

    [Header("아이콘")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _initial;
    [SerializeField] private GameObject _overlay;
    [SerializeField] private TMP_Text _overlayText;

    [Header("상태")]
    [SerializeField] private Color _normalColor = new Color(0.15f, 0.15f, 0.17f, 0.92f);
    [SerializeField] private Color _equippedColor = new Color(0.85f, 0.65f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float _dimmedAlpha = 0.4f;
    [SerializeField] private string _lockedText = "LOCKED";
    [SerializeField] private string _notReadyText = "SOON";

    private void Awake()
    {
        _button.onClick.AddListener(HandleClicked);
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        if (nameof(LoadoutItemViewModel.State) == propertyName)
        {
            DrawState();
        }
    }

    protected override void RefreshAll()
    {
        _label.text = ViewModel.Label;
        DrawIcon();
        DrawState();
    }

    private void DrawIcon()
    {
        Sprite icon = ViewModel.Icon;
        bool hasSprite = null != icon;

        _icon.sprite = icon;
        _icon.color = hasSprite ? Color.white : ViewModel.IconColor;

        _initial.gameObject.SetActive(false == hasSprite);
        _initial.text = 0 < ViewModel.Label.Length ? ViewModel.Label.Substring(0, 1) : string.Empty;
    }

    private void DrawState()
    {
        LoadoutItemState state = ViewModel.State;
        bool isLocked = LoadoutItemState.Locked == state;
        bool isUnavailable = isLocked || LoadoutItemState.NotReady == state;

        _background.color = LoadoutItemState.Equipped == state ? _equippedColor : _normalColor;
        _canvasGroup.alpha = isUnavailable ? _dimmedAlpha : 1f;

        _overlay.SetActive(isUnavailable);
        _overlayText.text = isLocked ? _lockedText : _notReadyText;
    }

    private void HandleClicked()
    {
        if (null != ViewModel)
        {
            ViewModel.Select();
        }
    }
}
