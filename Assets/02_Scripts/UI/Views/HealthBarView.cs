using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 체력바. 깎이면 흰 잔상이 남았다가 따라 줄고, 보스가 다시 일어나면 처음부터 차오른다. 기믹 줄 눈금과 기믹 중 색도 보인다.
public class HealthBarView : ViewBase<HealthBarViewModel>
{
    [SerializeField] private RectTransform _fill;
    [SerializeField] private RectTransform _trail;

    [Header("연출")]
    [SerializeField, Min(0f)] private float _fillDuration = 0.1f;
    [SerializeField, Min(0f)] private float _trailDelay = 0.25f;
    [SerializeField, Min(0f)] private float _trailDuration = 0.4f;
    [SerializeField] private Ease _trailEase = Ease.OutQuad;
    // 보스가 다시 일어나 체력이 처음부터 차오르는 시간.
    [SerializeField, Min(0f)] private float _refillDuration = 1f;

    [Header("기믹 눈금")]
    // 꺼 둔 눈금 하나. 줄 수만큼 복제해 체력 비율 자리에 세운다. 비워 두면 눈금을 그리지 않는다.
    [SerializeField] private RectTransform _markerTemplate;

    [Header("기믹 중")]
    // 보스가 기믹을 쓰는 동안 막대를 이 색으로 바꿔, 지금은 체력이 줄지 않는 시간임을 알린다.
    [SerializeField] private Color _alertColor = new Color(0.55f, 0.3f, 0.85f, 1f);

    private readonly List<RectTransform> _markers = new();

    private Graphic _fillGraphic;
    private Color _fillColor;

    private void Awake()
    {
        _fillGraphic = _fill.GetComponent<Graphic>();

        if (null != _fillGraphic)
        {
            _fillColor = _fillGraphic.color;
        }
    }

    // HUD가 닫혀 파괴될 때 잔상 트윈이 지연 중이면, 파괴된 막대에 값을 쓰려다 경고가 나므로 함께 끈다.
    protected override void OnDestroy()
    {
        _fill.DOKill();
        _trail.DOKill();

        base.OnDestroy();
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(HealthBarViewModel.Ratio):
                DrawRatio(ViewModel.LastChange);
                break;

            case nameof(HealthBarViewModel.Markers):
                DrawMarkers();
                break;

            case nameof(HealthBarViewModel.IsAlert):
                DrawAlert();
                break;

            default:
                RefreshAll();
                break;
        }
    }

    protected override void RefreshAll()
    {
        SetRatioImmediately(ViewModel.Ratio);
        DrawMarkers();
        DrawAlert();
    }

    private void DrawRatio(HealthBarChange change)
    {
        float ratio = ViewModel.Ratio;

        switch (change)
        {
            // 깎인 만큼 흰 잔상이 잠깐 남았다가 따라 줄어들어, 얼마나 맞았는지 눈에 보이게 한다.
            case HealthBarChange.Damaged:
                TweenFill(ratio);
                _trail.DOKill();
                _trail.DOAnchorMax(new Vector2(ratio, 1f), _trailDuration)
                    .SetDelay(_trailDelay)
                    .SetEase(_trailEase)
                    .SetUpdate(true);
                break;

            case HealthBarChange.Healed:
                TweenFill(ratio);
                _trail.DOKill();
                _trail.anchorMax = new Vector2(ratio, 1f);
                break;

            // 빈 막대에서 가득 찰 때까지 천천히 차올라, 보스가 다시 일어났음을 보여 준다.
            case HealthBarChange.Refilled:
                SetRatioImmediately(0f);
                _fill.DOAnchorMax(Vector2.one, _refillDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                _trail.DOAnchorMax(Vector2.one, _refillDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                break;
        }
    }

    // 체력 줄마다 눈금을 세워 기믹이 어디서 나올지 보이게 한다. 쓰지 않는 눈금은 꺼 두고 다음에 다시 쓴다.
    private void DrawMarkers()
    {
        if (null == _markerTemplate)
        {
            return;
        }

        IReadOnlyList<float> ratios = ViewModel.Markers;

        for (int i = 0; i < ratios.Count; i++)
        {
            if (i >= _markers.Count)
            {
                _markers.Add(Instantiate(_markerTemplate, _markerTemplate.parent));
            }

            RectTransform marker = _markers[i];
            marker.gameObject.SetActive(true);
            marker.anchorMin = new Vector2(ratios[i], 0f);
            marker.anchorMax = new Vector2(ratios[i], 1f);
            marker.anchoredPosition = Vector2.zero;
        }

        for (int i = ratios.Count; i < _markers.Count; i++)
        {
            _markers[i].gameObject.SetActive(false);
        }
    }

    private void DrawAlert()
    {
        if (null == _fillGraphic)
        {
            return;
        }

        _fillGraphic.color = ViewModel.IsAlert ? _alertColor : _fillColor;
    }

    private void TweenFill(float ratio)
    {
        _fill.DOKill();
        _fill.DOAnchorMax(new Vector2(ratio, 1f), _fillDuration).SetUpdate(true);
    }

    // 막대는 왼쪽에 고정하고 오른쪽 앵커만 체력 비율로 움직여, 스프라이트 없이도 해상도에 맞게 줄어든다.
    private void SetRatioImmediately(float ratio)
    {
        _fill.DOKill();
        _trail.DOKill();

        _fill.anchorMax = new Vector2(ratio, 1f);
        _trail.anchorMax = new Vector2(ratio, 1f);
    }
}
