using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

// 장비 화면 칸 하나의 규칙. 늘어놓을 항목을 만들고, 항목마다 가졌는지·끼웠는지·쓸 수 있는지와 끼우기를 PlayerLoadout에 물어 답한다.
// 칸 종류마다 읽는 표와 끼우는 규칙이 달라 하위 클래스로 나눈다. 새 칸은 규칙 하위 클래스 하나를 더하면 된다.
public abstract class LoadoutRule
{
    // 공격 방식·효과 색이 없는 항목의 임시 아이콘 색.
    protected static readonly Color DEFAULT_ICON_COLOR = new Color(0.6f, 0.6f, 0.65f, 1f);

    private readonly string _title;
    private readonly PlayerLoadout _loadout;
    private readonly AssetManager _assets;
    private readonly List<LoadoutEntry> _entries = new();

    public string Title { get { return _title; } }
    // 표 순서로 늘어놓아, 얻은 순서와 상관없이 늘 같은 자리에 보이게 한다.
    public IReadOnlyList<LoadoutEntry> Entries { get { return _entries; } }

    protected PlayerLoadout Loadout { get { return _loadout; } }
    protected AssetManager Assets { get { return _assets; } }

    protected LoadoutRule(string title, PlayerLoadout loadout, AssetManager assets)
    {
        _title = title;
        _loadout = loadout;
        _assets = assets;
    }

    public LoadoutItemState GetState(LoadoutEntry entry)
    {
        if (false == IsOwned(entry))
        {
            return LoadoutItemState.Locked;
        }

        if (false == IsReady(entry))
        {
            return LoadoutItemState.NotReady;
        }

        return IsEquipped(entry) ? LoadoutItemState.Equipped : LoadoutItemState.Owned;
    }

    public abstract void Equip(LoadoutEntry entry);

    protected abstract bool IsOwned(LoadoutEntry entry);

    protected abstract bool IsEquipped(LoadoutEntry entry);

    protected virtual bool IsReady(LoadoutEntry entry)
    {
        return true;
    }

    // 아이콘은 아직 없을 수 있어 조용히 찾고, 영상은 가져오기 때 검사한 키라 그대로 찾는다.
    protected LoadoutEntry AddEntry(string id, string label, string description, string iconKey, Color iconColor, string previewKey)
    {
        _assets.TryGet(iconKey, out Sprite icon);
        VideoClip preview = _assets.Get<VideoClip>(previewKey);
        LoadoutEntry entry = new LoadoutEntry(id, label, description, icon, iconColor, preview);

        _entries.Add(entry);

        return entry;
    }

    // 공격 방식 색을 아이콘 대신 쓴다. 키가 비었으면 기본색이다.
    protected Color GetStyleColor(string attackStyleKey)
    {
        AttackStyle style = _assets.Get<AttackStyle>(attackStyleKey);

        return null != style ? style.IconColor : DEFAULT_ICON_COLOR;
    }
}
