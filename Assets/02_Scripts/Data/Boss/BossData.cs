using UnityEngine;

// 보스 유닛 데이터. 공통 유닛 값(BattleUnitData)에 보스만 쓰는 값을 더한다.
[CreateAssetMenu(fileName = "BossData", menuName = "Scriptable Objects/BossData")]
public class BossData : BattleUnitData
{
    [Header("보스 이름 배너")]
    // 등장 컷신에서 띄운다. 쓰러졌다 일어나는 페이즈의 배너는 BossPhaseData에 따로 둔다.
    [SerializeField] private string _bannerTitle;
    [SerializeField] private string _bannerSubtitle;

    public string BannerTitle { get { return _bannerTitle; } }
    public string BannerSubtitle { get { return _bannerSubtitle; } }
}
