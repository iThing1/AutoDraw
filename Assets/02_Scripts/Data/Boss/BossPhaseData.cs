using System;
using UnityEngine;

// 보스 페이즈 하나. 체력, 쓰는 패턴, 맞을수록 물드는 색, 체력 줄마다 나오는 기믹을 담는다.
// 페이즈 순서와 전환은 BossPhaseController가 정하고, 여기는 값만 들고 있다.
[CreateAssetMenu(fileName = "BossPhaseData", menuName = "Scriptable Objects/Boss/PhaseData")]
public class BossPhaseData : ScriptableObject
{
    // 기믹 줄로 나뉜 체력 구간 하나의 색. 구간 위쪽(체력이 많을 때)이 From, 아래쪽이 To다.
    [Serializable]
    public class ColorSegment
    {
        [SerializeField] private Color _from = Color.gray;
        [SerializeField] private Color _to = Color.gray;

        public Color From { get { return _from; } }

        public Color Evaluate(float progress)
        {
            return Color.Lerp(_from, _to, progress);
        }
    }

    [Serializable]
    public class GimmickLine
    {
        [SerializeField, Range(0f, 1f)] private float _hpRatio = 0.5f;
        [SerializeField] private BossGimmick _gimmick;

        public float HpRatio { get { return _hpRatio; } }
        public BossGimmick Gimmick { get { return _gimmick; } }
    }

    [Header("체력")]
    // 보스 데이터의 최대 체력에 곱한다. 부활한 페이즈를 조금 짧게 할 때 1보다 작게 준다.
    [SerializeField, Min(0.01f)] private float _hpRatio = 1f;

    [Header("패턴")]
    [SerializeField] private AttackStyle _attackStyle;

    [Header("색")]
    // 기믹 줄로 나뉜 구간마다 하나씩, 위 구간부터 적는다. 줄이 n개면 n+1개다. 모자라면 마지막 구간 색을 이어 쓴다.
    [SerializeField] private ColorSegment[] _colorSegments;

    [Header("기믹")]
    // 체력이 많은 줄부터 적는다.
    [SerializeField] private GimmickLine[] _gimmickLines;

    [Header("등장 배너")]
    // 쓰러졌다 일어나 이 페이즈를 시작할 때 띄운다. 비워 두면 띄우지 않는다.
    [SerializeField] private string _bannerTitle;
    [SerializeField] private string _bannerSubtitle;

    public float HpRatio { get { return _hpRatio; } }
    public AttackStyle AttackStyle { get { return _attackStyle; } }
    public int GimmickLineCount { get { return null != _gimmickLines ? _gimmickLines.Length : 0; } }
    public string BannerTitle { get { return _bannerTitle; } }
    public string BannerSubtitle { get { return _bannerSubtitle; } }

    public GimmickLine GetGimmickLine(int index)
    {
        return _gimmickLines[index];
    }

    public ColorSegment GetColorSegment(int index)
    {
        if (null == _colorSegments || 0 == _colorSegments.Length)
        {
            return null;
        }

        return _colorSegments[Mathf.Clamp(index, 0, _colorSegments.Length - 1)];
    }

    // 구간 index의 위쪽 체력 비율. 첫 구간은 가득 찬 체력에서 시작한다.
    public float GetSegmentTop(int index)
    {
        return 0 == index ? 1f : _gimmickLines[index - 1].HpRatio;
    }

    // 구간 index의 아래쪽 체력 비율. 마지막 구간은 바닥까지 간다.
    public float GetSegmentBottom(int index)
    {
        return index < GimmickLineCount ? _gimmickLines[index].HpRatio : 0f;
    }
}
