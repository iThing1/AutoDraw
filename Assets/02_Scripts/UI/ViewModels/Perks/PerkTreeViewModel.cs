using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 특성 트리 화면에 보일 값. 노드마다 ViewModel을 만들어 두고, 특성이 바뀌면 상태와 남은 포인트만 다시 매긴다.
// 찍기는 노드를 고른 뒤 찍기 버튼으로 두 번에 나눠, 되돌릴 수 없는 선택을 실수로 하지 않게 한다.
public class PerkTreeViewModel : GalleryScreenViewModel
{
    private const string COST_FORMAT = "Cost {0}";
    private const string REQUIRES_FORMAT = "Requires: {0}";
    private const string NOT_ENOUGH_POINTS = "Not enough points.";
    private const string UNLOCKED_NOTE = "Unlocked.";
    private const string SELECT_HINT = "Select a perk to see what it does.";
    private const string NAME_SEPARATOR = ", ";
    // AppendLine은 Windows에서 \r\n을 넣으므로, 다른 설명 문구와 같게 \n만 쓴다.
    private const char NEW_LINE = '\n';

    private readonly PlayerModel _player;
    private readonly DataTable<PerkEffectData> _effects;
    private readonly List<PerkNodeViewModel> _nodes = new();
    private readonly Dictionary<string, PerkNodeViewModel> _nodesById = new();
    private readonly StringBuilder _builder = new();
    private int _availablePoints;
    private PerkNodeViewModel _selected;
    private string _selectedTitle = string.Empty;
    private string _selectedDescription = SELECT_HINT;

    // 특성이 바뀌어 노드 상태를 다시 매기면 이 이름으로 알린다.
    public IReadOnlyList<PerkNodeViewModel> Nodes { get { return _nodes; } }
    public int AvailablePoints { get { return _availablePoints; } }
    public string SelectedTitle { get { return _selectedTitle; } }
    public string SelectedDescription { get { return _selectedDescription; } }
    public bool CanUnlockSelected { get { return null != _selected && PerkNodeState.Available == _selected.State; } }

    public PerkTreeViewModel(PlayerModel player, DataTableManager tables, AssetManager assets)
    {
        _player = player;
        _effects = tables.PerkEffects;
        _availablePoints = player.AvailablePerkPoints;

        foreach (PerkNodeData node in player.PerkTree.Nodes)
        {
            assets.TryGet(node.IconKey, out Sprite icon);
            PerkNodeViewModel nodeViewModel = new PerkNodeViewModel(node, icon, player.GetPerkState(node.Id), HandleNodeSelected);

            _nodes.Add(nodeViewModel);
            _nodesById.Add(node.Id, nodeViewModel);
        }

        _player.OnPerksChanged += HandlePerksChanged;
    }

    public override void UnBind()
    {
        _player.OnPerksChanged -= HandlePerksChanged;

        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodes[i].UnBind();
        }
    }

    public bool TryGetNode(string nodeId, out PerkNodeViewModel node)
    {
        return _nodesById.TryGetValue(nodeId, out node);
    }

    public void UnlockSelected()
    {
        if (CanUnlockSelected)
        {
            _player.TryUnlockPerk(_selected.Id);
        }
    }

    // 찍으면 자식 노드가 열리고 남은 포인트가 줄어 다른 노드의 상태도 바뀌므로 전부 다시 매긴다.
    private void HandlePerksChanged()
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodes[i].SetState(_player.GetPerkState(_nodes[i].Id));
        }

        NotifyPropertyChanged(nameof(Nodes));

        _availablePoints = _player.AvailablePerkPoints;
        NotifyPropertyChanged(nameof(AvailablePoints));

        DescribeSelected();
    }

    private void HandleNodeSelected(PerkNodeViewModel node)
    {
        if (null != _selected)
        {
            _selected.SetSelected(false);
        }

        _selected = node;
        _selected.SetSelected(true);

        DescribeSelected();
    }

    // 고른 노드의 이름, 비용, 효과 줄, 찍을 수 없는 이유를 쓴다. 찍기 버튼도 이때 함께 다시 그린다.
    private void DescribeSelected()
    {
        if (null == _selected || false == _player.PerkTree.TryGetNode(_selected.Id, out PerkNodeData node))
        {
            return;
        }

        _builder.Clear();
        _builder.AppendFormat(COST_FORMAT, node.Cost.ToString()).Append(NEW_LINE).Append(NEW_LINE);

        for (int i = 0; i < node.EffectIds.Count && i < node.EffectValues.Count; i++)
        {
            if (_effects.TryGet(node.EffectIds[i], out PerkEffectData effect))
            {
                _builder.AppendFormat(effect.Description, node.EffectValues[i].ToString()).Append(NEW_LINE);
            }
        }

        string note = GetNote(node);

        if (false == string.IsNullOrEmpty(note))
        {
            _builder.Append(NEW_LINE).Append(note);
        }

        _selectedTitle = node.Name;
        _selectedDescription = _builder.ToString();

        NotifyPropertyChanged(nameof(SelectedTitle));
        NotifyPropertyChanged(nameof(SelectedDescription));
        NotifyPropertyChanged(nameof(CanUnlockSelected));
    }

    private string GetNote(PerkNodeData node)
    {
        switch (_selected.State)
        {
            case PerkNodeState.Unlocked:
                return UNLOCKED_NOTE;

            case PerkNodeState.Unaffordable:
                return NOT_ENOUGH_POINTS;

            case PerkNodeState.Locked:
                return string.Format(REQUIRES_FORMAT, GetMissingParentNames(node));

            default:
                return string.Empty;
        }
    }

    private string GetMissingParentNames(PerkNodeData node)
    {
        List<string> names = new List<string>();

        for (int i = 0; i < node.ParentIds.Count; i++)
        {
            if (_nodesById.TryGetValue(node.ParentIds[i], out PerkNodeViewModel parent) && PerkNodeState.Unlocked != parent.State)
            {
                names.Add(parent.Label);
            }
        }

        return string.Join(NAME_SEPARATOR, names);
    }
}
