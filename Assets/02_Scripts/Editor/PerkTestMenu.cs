using UnityEditor;
using UnityEngine;

// 특성 트리 화면이 생기기 전까지 특성을 시험하는 메뉴. 플레이 중에만 쓸 수 있고, 바꾼 것은 바로 저장한다.
public static class PerkTestMenu
{
    private const string MENU_ROOT = "AutoDraw/테스트/";
    private const int TEST_POINTS = 10;

    [MenuItem(MENU_ROOT + "특성 포인트 +10")]
    private static void AddPoints()
    {
        GameManager.Player.Progress.AddPoints(TEST_POINTS);
        Save($"포인트를 {TEST_POINTS} 더했습니다.");
    }

    // 하나를 찍으면 자식이 찍을 수 있게 되므로, 더 찍을 것이 없을 때까지 표를 다시 돈다.
    [MenuItem(MENU_ROOT + "찍을 수 있는 특성 모두 찍기")]
    private static void UnlockAvailable()
    {
        PlayerModel player = GameManager.Player;
        int count = 0;
        bool hasUnlocked = true;

        while (hasUnlocked)
        {
            hasUnlocked = false;

            foreach (PerkNodeData node in player.PerkTree.Nodes)
            {
                if (player.TryUnlockPerk(node.Id))
                {
                    count++;
                    hasUnlocked = true;
                }
            }
        }

        Save($"특성 {count}개를 찍었습니다.");
    }

    [MenuItem(MENU_ROOT + "특성 초기화")]
    private static void ResetPerks()
    {
        GameManager.Player.ResetPerks();
        Save("특성을 모두 되돌렸습니다.");
    }

    [MenuItem(MENU_ROOT + "특성 포인트 +10", true)]
    [MenuItem(MENU_ROOT + "찍을 수 있는 특성 모두 찍기", true)]
    [MenuItem(MENU_ROOT + "특성 초기화", true)]
    private static bool CanUse()
    {
        return Application.isPlaying && null != GameManager.Instance && null != GameManager.Player;
    }

    private static void Save(string message)
    {
        GameManager.SavePlayer();

        PlayerModel player = GameManager.Player;
        Debug.Log($"특성 시험 : {message} 남은 포인트 {player.AvailablePerkPoints}, 최대 체력 {player.Stats.MaxHp}, 이동 속도 {player.Stats.MoveSpeed}");
    }
}
