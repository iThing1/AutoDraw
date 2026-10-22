using System.Collections.Generic;
using UnityEngine.Video;

// 장비 화면에 보일 값. 칸 규칙(LoadoutRule)마다 버튼 ViewModel을 만들어 두고, 장비가 바뀌면 상태만 다시 매긴다.
// 화면을 여는 쪽이 만들고, 닫을 때 UnBind로 장비 구독을 푼다.
public class LoadoutViewModel : GalleryScreenViewModel
{
    private const string LOCKED_NOTE = "Locked.";
    private const string NOT_READY_NOTE = "Coming soon.";

    private readonly PlayerLoadout _loadout;
    private readonly List<LoadoutSlotViewModel> _slots = new();
    private string _description = string.Empty;
    private VideoClip _preview;

    public IReadOnlyList<LoadoutSlotViewModel> Slots { get { return _slots; } }
    // 마지막으로 고른 장비의 설명.
    public string Description { get { return _description; } }
    // 마지막으로 고른 장비의 공격 예시 영상. 없으면 null이다.
    public VideoClip Preview { get { return _preview; } }

    public LoadoutViewModel(PlayerModel player, DataTableManager tables, AssetManager assets)
    {
        PlayerLoadout loadout = player.Loadout;
        _loadout = loadout;

        // 화면 위에서부터 이 순서로 놓인다. LoadoutView의 칸 키도 같은 순서로 넣는다.
        LoadoutRule[] rules =
        {
            new WeaponLoadoutRule("Basic", WeaponType.Basic, loadout, tables, assets),
            new WeaponLoadoutRule("Heavy", WeaponType.Heavy, loadout, tables, assets),
            new SkillLoadoutRule(loadout, tables, assets),
            new UtilityLoadoutRule(loadout, assets),
            new ConsumableLoadoutRule(loadout, player.Stats, tables, assets),
        };

        for (int i = 0; i < rules.Length; i++)
        {
            _slots.Add(new LoadoutSlotViewModel(rules[i], HandleItemSelected));
        }

        _loadout.OnChanged += HandleLoadoutChanged;
    }

    public override void UnBind()
    {
        _loadout.OnChanged -= HandleLoadoutChanged;

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].UnBind();
        }
    }

    private void HandleLoadoutChanged()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].RefreshStates();
        }
    }

    private void HandleItemSelected(LoadoutItemViewModel item)
    {
        _description = Describe(item.Description, item.State);
        NotifyPropertyChanged(nameof(Description));

        _preview = item.Preview;
        NotifyPropertyChanged(nameof(Preview));
    }

    private static string Describe(string description, LoadoutItemState state)
    {
        string note = GetNote(state);

        if (string.IsNullOrEmpty(note))
        {
            return description;
        }

        return string.IsNullOrEmpty(description) ? note : $"{description}\n{note}";
    }

    private static string GetNote(LoadoutItemState state)
    {
        switch (state)
        {
            case LoadoutItemState.Locked:
                return LOCKED_NOTE;

            case LoadoutItemState.NotReady:
                return NOT_READY_NOTE;

            default:
                return string.Empty;
        }
    }
}
