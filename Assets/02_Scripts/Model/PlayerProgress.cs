using System.Collections.Generic;

// 깬 스테이지와 얻은 특성 포인트. 남은 포인트는 찍은 특성의 비용을 빼서 구해, 표의 비용을 바꿔도 저장과 어긋나지 않게 한다.
public class PlayerProgress
{
    private readonly List<string> _clearedStageKeys = new();
    private int _earnedPoints;

    public IReadOnlyList<string> ClearedStageKeys { get { return _clearedStageKeys; } }
    public int EarnedPoints { get { return _earnedPoints; } }

    private PlayerProgress()
    {
    }

    public static PlayerProgress CreateNew()
    {
        return new PlayerProgress();
    }

    // 진행을 저장하기 전 파일이면 save가 null이라 처음 상태로 시작한다.
    public static PlayerProgress FromSave(ProgressSaveData save)
    {
        PlayerProgress progress = new();

        if (null != save)
        {
            foreach (string stageKey in save.ClearedStageKeys)
            {
                progress.AddCleared(stageKey);
            }

            progress._earnedPoints = save.EarnedPoints;
        }

        return progress;
    }

    public ProgressSaveData ToSaveData()
    {
        return new ProgressSaveData(this);
    }

    public bool IsCleared(string stageKey)
    {
        return _clearedStageKeys.Contains(stageKey);
    }

    // 처음 깬 스테이지만 기록하고 포인트를 더한다. 이미 깬 곳이면 false다.
    public bool TryMarkCleared(string stageKey, int rewardPoints)
    {
        if (false == AddCleared(stageKey))
        {
            return false;
        }

        _earnedPoints += rewardPoints;

        return true;
    }

    private bool AddCleared(string stageKey)
    {
        if (string.IsNullOrEmpty(stageKey) || _clearedStageKeys.Contains(stageKey))
        {
            return false;
        }

        _clearedStageKeys.Add(stageKey);

        return true;
    }
}
