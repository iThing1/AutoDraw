using System;
using System.Collections.Generic;
using UnityEngine;

// 전시실 하나. 스테이지를 여는 순서와, 모두 깼을 때 완성을 알릴 문구를 담는다.
[CreateAssetMenu(fileName = "RoomData", menuName = "Scriptable Objects/RoomData")]
public class RoomData : ScriptableObject
{
    [SerializeField] private string _displayName;
    // 완성 기록에 쓰는 키. 표시 이름을 바꿔도 기록이 유지된다.
    [SerializeField] private string _saveKey;
    [SerializeField] private StageData[] _stages;

    [Header("완성 연출")]
    [SerializeField] private string _completeTitle = "ROOM COMPLETE";
    [SerializeField] private string _completeSubtitle;

    [Header("다음 전시실")]
    // 문을 열었을 때 띄우는 안내.
    [SerializeField] private string _nextRoomNotice;

    public string DisplayName { get { return _displayName; } }
    public string SaveKey { get { return _saveKey; } }
    public IReadOnlyList<StageData> Stages { get { return _stages; } }
    public string CompleteTitle { get { return _completeTitle; } }
    public string CompleteSubtitle { get { return _completeSubtitle; } }
    public string NextRoomNotice { get { return _nextRoomNotice; } }

    // 이미 깬 곳도 열어 두어, 순서 규칙이 없던 저장의 그림이 다시 천에 덮이지 않게 한다.
    public bool IsOpen(StageData stage, PlayerProgress progress)
    {
        // 스테이지가 비어 있는 액자는 원래 잠겨 있는 자리다.
        if (null == stage)
        {
            return false;
        }

        int index = Array.IndexOf(_stages, stage);

        if (0 > index)
        {
            Debug.LogError($"전시실 : {name}의 스테이지 목록에 {stage.name}이(가) 없어 액자를 잠급니다.");
            return false;
        }

        return 0 == index || progress.IsCleared(stage.SaveKey) || progress.IsCleared(_stages[index - 1].SaveKey);
    }

    public bool IsComplete(PlayerProgress progress)
    {
        if (0 == _stages.Length)
        {
            return false;
        }

        for (int i = 0; i < _stages.Length; i++)
        {
            if (false == progress.IsCleared(_stages[i].SaveKey))
            {
                return false;
            }
        }

        return true;
    }
}
