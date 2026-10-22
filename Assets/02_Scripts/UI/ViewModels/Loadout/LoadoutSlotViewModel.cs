using System;
using System.Collections.Generic;

// 장비 화면의 칸 한 줄에 보일 값. 칸 이름과, 칸의 항목마다 만든 버튼 ViewModel.
public class LoadoutSlotViewModel : ViewModelBase
{
    private readonly string _title;
    private readonly List<LoadoutItemViewModel> _items = new();

    public string Title { get { return _title; } }
    public IReadOnlyList<LoadoutItemViewModel> Items { get { return _items; } }

    public LoadoutSlotViewModel(LoadoutRule rule, Action<LoadoutItemViewModel> onItemSelected)
    {
        _title = rule.Title;

        for (int i = 0; i < rule.Entries.Count; i++)
        {
            _items.Add(new LoadoutItemViewModel(rule.Entries[i], rule, onItemSelected));
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
