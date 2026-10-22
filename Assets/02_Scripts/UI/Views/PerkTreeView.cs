using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 갤러리 이젤에서 여는 특성 트리 화면. 연결할 때 노드와 연결선을 표의 자리(트리·행·열)대로 한 번 만들고,
// 그 뒤로는 남은 포인트, 고른 노드의 설명, 찍기 버튼, 연결선 색만 다시 그린다.
public class PerkTreeView : ViewBase<PerkTreeViewModel>
{
    private static readonly int TREE_COUNT = Enum.GetValues(typeof(PerkTreeType)).Length;
    private const int CENTER_COLUMN = 1;

    [SerializeField] private TMP_Text _pointsText;
    [SerializeField] private string _pointsFormat = "POINTS {0}";
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Button _unlockButton;
    [SerializeField] private Button _closeButton;

    [Header("트리")]
    // 위 가운데를 기준점으로 둔 영역. 노드는 첫 줄이 맨 위, 가운데 트리가 가운데에 온다.
    [SerializeField] private RectTransform _nodeRoot;
    // 연결선이 노드 밑에 깔리도록 노드 영역보다 앞(형제 순서)에 둔다.
    [SerializeField] private RectTransform _lineRoot;
    [SerializeField] private PerkNodeView _nodePrefab;
    [SerializeField] private Image _linePrefab;
    // 같은 트리 안에서 열·행 사이 간격.
    [SerializeField] private Vector2 _cellSpacing = new Vector2(96f, 124f);
    [SerializeField, Min(0f)] private float _treeSpacing = 300f;
    [SerializeField, Min(0f)] private float _lineWidth = 4f;

    [Header("색")]
    // PerkTreeType 순서(Stat, Equipment, Technique)로 넣는다.
    [SerializeField] private Color[] _treeColors =
    {
        new Color(0.85f, 0.4f, 0.35f, 1f),
        new Color(0.4f, 0.65f, 0.85f, 1f),
        new Color(0.45f, 0.75f, 0.45f, 1f),
    };
    [SerializeField] private Color _lineColor = new Color(0.45f, 0.45f, 0.5f, 1f);
    [SerializeField] private Color _unlockedLineColor = new Color(0.85f, 0.65f, 0.3f, 1f);

    private readonly List<PerkNodeView> _nodeViews = new();
    private readonly Dictionary<string, PerkNodeView> _nodeViewsById = new();
    private readonly List<Image> _lines = new();
    // 선마다 이어진 자식 노드. 자식을 찍었으면 그 길로 온 것이라 선을 금색으로 칠한다.
    private readonly List<PerkNodeViewModel> _lineChildren = new();

    private void Awake()
    {
        _closeButton.onClick.AddListener(HandleCloseClicked);
        _unlockButton.onClick.AddListener(HandleUnlockClicked);
    }

    protected override void OnBind()
    {
        IReadOnlyList<PerkNodeViewModel> nodes = ViewModel.Nodes;

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateNode(nodes[i]);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateLines(nodes[i]);
        }
    }

    // 다른 ViewModel을 다시 꽂을 때 옛 노드와 선이 남지 않게 지운다. 화면째 지워질 때는 먼저 지워졌을 수 있다.
    protected override void OnUnbind()
    {
        for (int i = 0; i < _nodeViews.Count; i++)
        {
            if (null != _nodeViews[i])
            {
                Destroy(_nodeViews[i].gameObject);
            }
        }

        for (int i = 0; i < _lines.Count; i++)
        {
            if (null != _lines[i])
            {
                Destroy(_lines[i].gameObject);
            }
        }

        _nodeViews.Clear();
        _nodeViewsById.Clear();
        _lines.Clear();
        _lineChildren.Clear();
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(PerkTreeViewModel.Nodes):
                DrawLines();
                break;

            case nameof(PerkTreeViewModel.AvailablePoints):
                DrawPoints();
                break;

            case nameof(PerkTreeViewModel.SelectedTitle):
                _titleText.text = ViewModel.SelectedTitle;
                break;

            case nameof(PerkTreeViewModel.SelectedDescription):
                _descriptionText.text = ViewModel.SelectedDescription;
                break;

            case nameof(PerkTreeViewModel.CanUnlockSelected):
                _unlockButton.interactable = ViewModel.CanUnlockSelected;
                break;
        }
    }

    protected override void RefreshAll()
    {
        DrawPoints();
        DrawLines();
        _titleText.text = ViewModel.SelectedTitle;
        _descriptionText.text = ViewModel.SelectedDescription;
        _unlockButton.interactable = ViewModel.CanUnlockSelected;
    }

    private void CreateNode(PerkNodeViewModel node)
    {
        PerkNodeView nodeView = Instantiate(_nodePrefab, _nodeRoot);
        nodeView.Rect.anchoredPosition = GetNodePosition(node);
        nodeView.SetTreeColor(GetTreeColor(node.Tree));
        nodeView.Bind(node);

        _nodeViews.Add(nodeView);
        _nodeViewsById.Add(node.Id, nodeView);
    }

    // 부모마다 선 하나. 두 노드의 가운데를 잇도록 선을 늘이고 돌린다.
    private void CreateLines(PerkNodeViewModel child)
    {
        Vector2 to = _nodeViewsById[child.Id].Rect.anchoredPosition;

        for (int i = 0; i < child.ParentIds.Count; i++)
        {
            if (false == _nodeViewsById.TryGetValue(child.ParentIds[i], out PerkNodeView parent))
            {
                continue;
            }

            Vector2 from = parent.Rect.anchoredPosition;
            Vector2 delta = to - from;

            Image line = Instantiate(_linePrefab, _lineRoot);
            RectTransform rect = line.rectTransform;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, _lineWidth);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            _lines.Add(line);
            _lineChildren.Add(child);
        }
    }

    // 트리는 가로로 나란히 가운데 맞추고, 트리 안에서는 열이 가로·행이 세로다. 열은 0~2이고 1이 트리의 가운데다.
    private Vector2 GetNodePosition(PerkNodeViewModel node)
    {
        float treeX = ((int)node.Tree - (TREE_COUNT - 1) * 0.5f) * _treeSpacing;
        float x = treeX + (node.Column - CENTER_COLUMN) * _cellSpacing.x;
        float y = -(node.Row + 0.5f) * _cellSpacing.y;

        return new Vector2(x, y);
    }

    private Color GetTreeColor(PerkTreeType tree)
    {
        int index = (int)tree;

        return 0 <= index && index < _treeColors.Length ? _treeColors[index] : Color.white;
    }

    private void DrawPoints()
    {
        _pointsText.SetText(_pointsFormat, ViewModel.AvailablePoints);
    }

    private void DrawLines()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            _lines[i].color = PerkNodeState.Unlocked == _lineChildren[i].State ? _unlockedLineColor : _lineColor;
        }
    }

    private void HandleUnlockClicked()
    {
        if (null != ViewModel)
        {
            ViewModel.UnlockSelected();
        }
    }

    private void HandleCloseClicked()
    {
        if (null != ViewModel)
        {
            ViewModel.RequestClose();
        }
    }
}
