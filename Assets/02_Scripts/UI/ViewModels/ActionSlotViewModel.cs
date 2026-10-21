using System;
using UnityEngine;

// 하단 HUD 칸 하나(공격·스킬·막기·소모품)에 보일 값. 키 이름·아이콘 색은 처음에 정해지고, 선택 여부·게이지·이름은 하위 클래스가 모델을 보고 바꾼다.
// 칸마다 데이터를 주는 곳(기본 공격 선택, 스킬 게이지, 막기 비용, 소모품 횟수)이 달라 하위 클래스로 나누고, 그리는 View는 하나를 같이 쓴다.
public abstract class ActionSlotViewModel : ViewModelBase
{
    private readonly string _keyLabel;
    private string _displayName;
    private readonly Color _iconColor;
    private readonly bool _usesGauge;
    private bool _isSelected;
    private float _gaugeRatio = 1f;
    private bool _isReady = true;

    // 칸을 쓴 순간(스킬 발동, 막기 시작). 한 번 튀는 표시만 하고 끝나는 사건이라 이름 알림 대신 이벤트로 낸다.
    public event Action OnUsed;

    public string KeyLabel { get { return _keyLabel; } }
    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }
    // 게이지를 쓰는 칸(스킬·막기)만 채움과 흐림을 보인다.
    public bool UsesGauge { get { return _usesGauge; } }
    public bool IsSelected { get { return _isSelected; } }
    public float GaugeRatio { get { return _gaugeRatio; } }
    // 게이지가 다 차 지금 쓸 수 있는지.
    public bool IsReady { get { return _isReady; } }

    protected ActionSlotViewModel(string keyLabel, AttackStyle style, bool usesGauge)
        : this(keyLabel, null != style ? style.DisplayName : string.Empty, null != style ? style.IconColor : Color.white, usesGauge)
    {
    }

    protected ActionSlotViewModel(string keyLabel, string displayName, Color iconColor, bool usesGauge)
    {
        _keyLabel = keyLabel;
        _displayName = displayName;
        _iconColor = iconColor;
        _usesGauge = usesGauge;
    }

    // 남은 횟수처럼 이름에 붙는 값이 바뀔 때 쓴다.
    protected void SetDisplayName(string displayName)
    {
        if (_displayName == displayName)
        {
            return;
        }

        _displayName = displayName;
        NotifyPropertyChanged(nameof(DisplayName));
    }

    protected void SetSelected(bool isSelected)
    {
        if (_isSelected == isSelected)
        {
            return;
        }

        _isSelected = isSelected;
        NotifyPropertyChanged(nameof(IsSelected));
    }

    // 비율과 사용 가능 여부는 함께 바뀌므로 한 번에 알린다.
    protected void SetGauge(float ratio, bool isReady)
    {
        _gaugeRatio = ratio;
        _isReady = isReady;
        NotifyPropertyChanged(nameof(GaugeRatio));
    }

    protected void NotifyUsed()
    {
        OnUsed?.Invoke();
    }
}
