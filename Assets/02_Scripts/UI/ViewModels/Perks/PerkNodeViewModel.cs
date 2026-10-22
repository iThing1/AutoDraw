using System;
using System.Collections.Generic;
using UnityEngine;

// 특성 트리 화면의 노드 하나에 보일 값. 자리(트리·행·열)와 이름은 표에서, 상태는 트리 화면 ViewModel이 매번 매겨 준다.
public class PerkNodeViewModel : ViewModelBase
{
    private readonly PerkNodeData _data;
    private readonly Sprite _icon;
    private readonly Action<PerkNodeViewModel> _onSelected;
    private PerkNodeState _state;
    private bool _isSelected;

    public string Id { get { return _data.Id; } }
    public string Label { get { return _data.Name; } }
    public PerkTreeType Tree { get { return _data.Tree; } }
    public int Row { get { return _data.Row; } }
    public int Column { get { return _data.Column; } }
    public int Cost { get { return _data.Cost; } }
    public IReadOnlyList<string> ParentIds { get { return _data.ParentIds; } }
    // 아이콘 그림이 아직 없으면 null이고, 화면은 색 타일로 대신 그린다.
    public Sprite Icon { get { return _icon; } }
    public PerkNodeState State { get { return _state; } }
    public bool IsSelected { get { return _isSelected; } }

    public PerkNodeViewModel(PerkNodeData data, Sprite icon, PerkNodeState state, Action<PerkNodeViewModel> onSelected)
    {
        _data = data;
        _icon = icon;
        _state = state;
        _onSelected = onSelected;
    }

    // 모델을 직접 구독하지 않아 풀 것이 없다.
    public override void UnBind()
    {
    }

    public void SetState(PerkNodeState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        NotifyPropertyChanged(nameof(State));
    }

    public void SetSelected(bool isSelected)
    {
        if (_isSelected == isSelected)
        {
            return;
        }

        _isSelected = isSelected;
        NotifyPropertyChanged(nameof(IsSelected));
    }

    // 잠긴 노드도 설명은 볼 수 있게 고를 수 있다. 찍는 것은 트리 화면의 찍기 버튼으로 한다.
    public void Select()
    {
        _onSelected(this);
    }
}
