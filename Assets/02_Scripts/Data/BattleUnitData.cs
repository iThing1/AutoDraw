using UnityEngine;

[CreateAssetMenu(fileName = "BattleUnitData", menuName = "Scriptable Objects/BattleUnitData")]
public class BattleUnitData : ScriptableObject
{
    [Header("이동")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;

    [Header("보스 이름 배너")]
    [SerializeField] private string _bannerTitle;
    [SerializeField] private string _bannerSubtitle;

    public float MoveSpeed { get { return _moveSpeed; } }

    public string BannerTitle { get { return _bannerTitle; } }
    public string BannerSubtitle { get { return _bannerSubtitle; } }
}
