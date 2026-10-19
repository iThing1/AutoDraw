using UnityEngine;

// 보스 유닛 데이터. 공통 유닛 값(BattleUnitData)에 보스만 쓰는 값을 더한다. 보스 스탯은 SO에 두므로 이 에셋이 곧 스탯이다.
[CreateAssetMenu(fileName = "BossData", menuName = "Scriptable Objects/BossData")]
public class BossData : BattleUnitData, IUnitStats
{
    [Header("체력")]
    [SerializeField, Min(1)] private int _maxHp = 100;
    // 떨어진 자리가 몸 중심에서 이 거리 안이면 맞는다. 몸보다 넉넉하게 준다.
    [SerializeField, Min(0f)] private float _hitRadius = 1f;

    [Header("보스 이름 배너")]
    // 등장 컷신에서 띄운다. 쓰러졌다 일어나는 페이즈의 배너는 BossPhaseData에 따로 둔다.
    [SerializeField] private string _bannerTitle;
    [SerializeField] private string _bannerSubtitle;

    public override IUnitStats Stats { get { return this; } }

    public int MaxHp { get { return _maxHp; } }
    public float HitRadius { get { return _hitRadius; } }

    public string BannerTitle { get { return _bannerTitle; } }
    public string BannerSubtitle { get { return _bannerSubtitle; } }
}
