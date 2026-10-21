using UnityEngine;

// 갤러리 액자 하나에 걸리는 스테이지. 액자를 고르면 이 전투 스테이지로 들어간다.
[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private BattleManager _stagePrefab;
    // 깨기 전에 액자에 거는 빈 도화지. 깨면 그 판에서 칠한 그림으로 바뀐다.
    [SerializeField] private Texture2D _blankCanvas;
    // 진행 기록과 클리어 그림 파일에 쓰는 이 스테이지의 키. 표시 이름을 바꿔도 기록이 이어지도록 따로 둔다.
    [SerializeField] private string _saveKey;

    [Header("클리어 보상")]
    // 처음 깼을 때만 받는다.
    [SerializeField, Min(0)] private int _rewardPoints;
    [SerializeField] private string _clearSkillId;

    public string DisplayName { get { return _displayName; } }
    public BattleManager StagePrefab { get { return _stagePrefab; } }
    public Texture2D BlankCanvas { get { return _blankCanvas; } }
    public string SaveKey { get { return _saveKey; } }
    public int RewardPoints { get { return _rewardPoints; } }
    public string ClearSkillId { get { return _clearSkillId; } }
}
