using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 장비 화면의 칸 한 줄. 연결할 때 칸의 장비 버튼을 한 번 만든다.
public class LoadoutSlotView : ViewBase<LoadoutSlotViewModel>
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Transform _itemRoot;
    [SerializeField] private LoadoutItemView _itemPrefab;
    // {0}은 칸 이름, {1}은 그 칸의 키 이름. 키가 없는 칸은 이름만 쓴다.
    [SerializeField] private string _titleFormat = "{0} <{1}>";

    private readonly List<LoadoutItemView> _itemViews = new();
    private string _keyLabel = string.Empty;

    // Bind 전에 넣어 두면 연결할 때 함께 그린다.
    public void SetKeyLabel(string keyLabel)
    {
        _keyLabel = keyLabel;
    }

    protected override void OnBind()
    {
        IReadOnlyList<LoadoutItemViewModel> items = ViewModel.Items;

        for (int i = 0; i < items.Count; i++)
        {
            LoadoutItemView itemView = Instantiate(_itemPrefab, _itemRoot);
            itemView.Bind(items[i]);
            _itemViews.Add(itemView);
        }
    }

    // 다른 ViewModel을 다시 꽂을 때 옛 버튼이 남지 않게 지운다. 화면째 지워질 때는 버튼이 먼저 지워졌을 수 있다.
    protected override void OnUnbind()
    {
        for (int i = 0; i < _itemViews.Count; i++)
        {
            if (null != _itemViews[i])
            {
                Destroy(_itemViews[i].gameObject);
            }
        }

        _itemViews.Clear();
    }

    protected override void OnPropertyChanged(string propertyName)
    {
    }

    protected override void RefreshAll()
    {
        _title.text = string.IsNullOrEmpty(_keyLabel) ? ViewModel.Title : string.Format(_titleFormat, ViewModel.Title, _keyLabel);
    }
}
