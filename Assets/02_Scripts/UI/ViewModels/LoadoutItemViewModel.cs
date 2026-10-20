using System;
using UnityEngine;
using UnityEngine.Video;

// 장비 화면 버튼 하나에 보일 값. 이름·설명·아이콘은 처음에 정해지고, 상태는 장비가 바뀔 때마다 다시 매긴다.
// 무엇을 끼우고 어떻게 상태를 매기는지는 만드는 쪽(LoadoutViewModel)이 넘겨준다.
public class LoadoutItemViewModel : ViewModelBase
{
    private readonly string _label;
    private readonly string _description;
    private readonly Sprite _icon;
    private readonly Color _iconColor;
    private readonly VideoClip _preview;
    private readonly Func<LoadoutItemState> _getState;
    private readonly Action _equip;
    private readonly Action<LoadoutItemViewModel> _onSelected;
    private LoadoutItemState _state;

    public string Label { get { return _label; } }
    public string Description { get { return _description; } }
    // 아이콘 그림이 아직 없으면 null이고, View는 색 타일로 대신 그린다.
    public Sprite Icon { get { return _icon; } }
    public Color IconColor { get { return _iconColor; } }
    // 공격 예시 영상. 아직 녹화하지 않았으면 null이다.
    public VideoClip Preview { get { return _preview; } }
    public LoadoutItemState State { get { return _state; } }

    public LoadoutItemViewModel(string label, string description, Sprite icon, Color iconColor, VideoClip preview,
        Func<LoadoutItemState> getState, Action equip, Action<LoadoutItemViewModel> onSelected)
    {
        _label = label;
        _description = description;
        _icon = icon;
        _iconColor = iconColor;
        _preview = preview;
        _getState = getState;
        _equip = equip;
        _onSelected = onSelected;
        _state = getState();
    }

    // 모델을 직접 구독하지 않아 풀 것이 없다.
    public override void UnBind()
    {
    }

    public void RefreshState()
    {
        LoadoutItemState state = _getState();

        if (_state == state)
        {
            return;
        }

        _state = state;
        NotifyPropertyChanged(nameof(State));
    }

    // 잠기거나 준비 중인 장비도 설명은 볼 수 있게 고를 수는 있고, 끼우는 것만 막는다.
    public void Select()
    {
        if (LoadoutItemState.Owned == _state)
        {
            _equip();
        }

        _onSelected(this);
    }
}
