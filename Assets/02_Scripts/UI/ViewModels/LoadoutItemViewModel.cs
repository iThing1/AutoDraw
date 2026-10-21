using System;
using UnityEngine;
using UnityEngine.Video;

// 장비 화면 버튼 하나에 보일 값. 그릴 값은 항목(LoadoutEntry)에서, 상태와 끼우기는 그 항목이 속한 칸(LoadoutSlot)에서 받는다.
public class LoadoutItemViewModel : ViewModelBase
{
    private readonly LoadoutEntry _entry;
    private readonly LoadoutSlot _slot;
    private readonly Action<LoadoutItemViewModel> _onSelected;
    private LoadoutItemState _state;

    public string Label { get { return _entry.Label; } }
    public string Description { get { return _entry.Description; } }
    public Sprite Icon { get { return _entry.Icon; } }
    public Color IconColor { get { return _entry.IconColor; } }
    public VideoClip Preview { get { return _entry.Preview; } }
    public LoadoutItemState State { get { return _state; } }

    public LoadoutItemViewModel(LoadoutEntry entry, LoadoutSlot slot, Action<LoadoutItemViewModel> onSelected)
    {
        _entry = entry;
        _slot = slot;
        _onSelected = onSelected;
        _state = slot.GetState(entry);
    }

    // 모델을 직접 구독하지 않아 풀 것이 없다.
    public override void UnBind()
    {
    }

    public void RefreshState()
    {
        LoadoutItemState state = _slot.GetState(_entry);

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
            _slot.Equip(_entry);
        }

        _onSelected(this);
    }
}
