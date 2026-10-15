using UnityEngine;

// 갤러리 액자 하나에 걸리는 스테이지. 액자를 고르면 이 전투 스테이지로 들어간다.
[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private BattleManager _stagePrefab;
    // 깨기 전에 액자에 거는 빈 도화지. 깨면 그 판에서 칠한 그림으로 바뀐다.
    [SerializeField] private Texture2D _blankCanvas;
    // 클리어 그림을 저장하는 파일 이름. 표시 이름을 바꿔도 그림이 이어지도록 따로 둔다.
    [SerializeField] private string _saveKey;

    public string DisplayName { get { return _displayName; } }
    public BattleManager StagePrefab { get { return _stagePrefab; } }
    public Texture2D BlankCanvas { get { return _blankCanvas; } }
    public string SaveKey { get { return _saveKey; } }
}
