using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 하단 HUD의 공격 아이콘 한 칸. 기본 공격은 선택 표시, 특수 스킬은 게이지 채움을 보여 준다.
public class AttackSlotUI : MonoBehaviour
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
    private bool _wasFull;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        _rect.DOKill();
    }

    public void Setup(string keyLabel, AttackStyle style)
    {
        string displayName = null != style ? style.DisplayName : string.Empty;
        Color iconColor = null != style ? style.IconColor : Color.white;

        Setup(keyLabel, displayName, iconColor);
    }

    public void Setup(string keyLabel, string displayName, Color iconColor)
    {
        _keyText.text = keyLabel;
        _nameText.text = displayName;
        _icon.color = iconColor;

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
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
    public void SetGauge(float ratio, bool isFull)
    {
        if (null != _gaugeFill)
        {
            _gaugeFill.anchorMax = new Vector2(1f, ratio);
        }

        Color color = _icon.color;
        color.a = isFull ? 1f : _dimmedAlpha;
        _icon.color = color;

        if (isFull && false == _wasFull)
        {
            Punch();
        }

        _wasFull = isFull;
    }

    public void PlayUsed()
    {
        Punch();
    }

    private void Punch()
    {
        _rect.DOComplete();
        _rect.DOPunchScale(Vector3.one * _selectPunch, _punchDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY).SetUpdate(true);
    }
}
