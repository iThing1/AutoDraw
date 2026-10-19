using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 맞을수록 물드는 보스 몸 색. 기믹 뒤나 부활 때는 한 단계 크게 물든다.
// 어느 구간인지는 페이즈를 아는 BossPhaseMachine이 넘긴다.
public class BossTint
{
    private readonly UnitView _view;
    private Color _current;
    private bool _isShifting;

    public BossTint(UnitView view)
    {
        _view = view;
    }

    public Color Current { get { return _current; } }

    // 구간 안에서 체력이 깎인 만큼 시작 색에서 끝 색으로 물든다. 구간 색이 없으면 지금 색을 그대로 둔다.
    public Color Evaluate(BossPhaseData phase, int segmentIndex, float hpRatio)
    {
        BossPhaseData.ColorSegment segment = phase.GetColorSegment(segmentIndex);

        if (null == segment)
        {
            return _current;
        }

        float top = phase.GetSegmentTop(segmentIndex);
        float bottom = phase.GetSegmentBottom(segmentIndex);

        return segment.Evaluate(Mathf.InverseLerp(top, bottom, hpRatio));
    }

    public Color GetSegmentStart(BossPhaseData phase, int segmentIndex)
    {
        BossPhaseData.ColorSegment segment = phase.GetColorSegment(segmentIndex);

        return null != segment ? segment.From : _current;
    }

    // 맞을 때마다 부른다. 크게 물드는 중이면 그 트윈이 색을 정하므로 건너뛴다.
    public void Follow(Color color)
    {
        if (_isShifting)
        {
            return;
        }

        Apply(color);
    }

    public void Apply(Color color)
    {
        _current = color;
        _view.SetBaseColor(color);
    }

    // 기믹 뒤에는 게임 시간, 부활 연출은 실제 시간으로 돈다.
    public async UniTask ShiftAsync(Color from, Color to, float duration, bool isRealtime, CancellationToken token)
    {
        _isShifting = true;

        try
        {
            await DOVirtual.Color(from, to, duration, Apply)
                .SetUpdate(isRealtime)
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        finally
        {
            _isShifting = false;
        }
    }
}
