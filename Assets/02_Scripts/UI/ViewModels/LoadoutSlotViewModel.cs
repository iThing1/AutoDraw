using System.Collections.Generic;

// 장비 화면의 칸 한 줄에 보일 값. 칸 이름과 그 칸의 장비들.
public class LoadoutSlotViewModel : ViewModelBase
{
    private readonly string _title;
    private readonly List<LoadoutItemViewModel> _items;

    public string Title { get { return _title; } }
    public IReadOnlyList<LoadoutItemViewModel> Items { get { return _items; } }

    public LoadoutSlotViewModel(string title, List<LoadoutItemViewModel> items)
    {
        _title = title;
        _items = items;
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
