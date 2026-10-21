using System;
using System.Collections.Generic;

// 장비 화면의 칸 한 줄에 보일 값. 칸 이름과, 칸의 항목마다 만든 버튼 ViewModel.
public class LoadoutSlotViewModel : ViewModelBase
{
    private readonly string _title;
    private readonly List<LoadoutItemViewModel> _items = new();

    public string Title { get { return _title; } }
    public IReadOnlyList<LoadoutItemViewModel> Items { get { return _items; } }

    public LoadoutSlotViewModel(LoadoutSlot slot, Action<LoadoutItemViewModel> onItemSelected)
    {
        _title = slot.Title;

        for (int i = 0; i < slot.Entries.Count; i++)
        {
            _items.Add(new LoadoutItemViewModel(slot.Entries[i], slot, onItemSelected));
        }
    }

    public override void UnBind()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].UnBind();
        }
    }

    public void RefreshStates()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].RefreshState();
        }
    }
}
