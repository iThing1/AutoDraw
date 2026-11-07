using UnityEditor;
using UnityEngine;

// 보스를 차례로 깨지 않고 순서 잠금과 전시실 완성 연출을 시험하는 메뉴. 바뀐 내용은 다음에 갤러리로 돌아올 때 보인다.
public static class RoomTestMenu
{
    private const string MENU_ROOT = "AutoDraw/테스트/";
    private const string CLEAR_NEXT_MENU = MENU_ROOT + "다음 스테이지 하나 깬 것으로 표시";
    private const string CLEAR_ALL_MENU = MENU_ROOT + "모든 스테이지 깬 것으로 표시";
    private const string FORGET_MENU = MENU_ROOT + "전시실 완성 기록 지우기";

    // 액자 열림 연출을 볼 때 쓴다. 전시실마다 아직 안 깬 첫 스테이지를 깬다.
    [MenuItem(CLEAR_NEXT_MENU)]
    private static void ClearNextStage()
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoomData)}"))
        {
            RoomData room = AssetDatabase.LoadAssetAtPath<RoomData>(AssetDatabase.GUIDToAssetPath(guid));

            for (int i = 0; i < room.Stages.Count; i++)
            {
                if (GameManager.Player.ClaimFirstClear(room.Stages[i]))
                {
                    Debug.Log($"전시실 시험 : {room.Stages[i].name}을(를) 깬 것으로 표시했습니다.");
                    break;
                }
            }
        }

        GameManager.SavePlayer();
    }

    [MenuItem(CLEAR_ALL_MENU)]
    private static void ClearAllStages()
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(StageData)}"))
        {
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid));
            GameManager.Player.ClaimFirstClear(stage);
        }

        GameManager.SavePlayer();
        Debug.Log("전시실 시험 : 모든 스테이지를 깬 것으로 표시했습니다.");
    }

    [MenuItem(FORGET_MENU)]
    private static void ForgetCompletedRooms()
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoomData)}"))
        {
            RoomData room = AssetDatabase.LoadAssetAtPath<RoomData>(AssetDatabase.GUIDToAssetPath(guid));
            GameManager.Player.Progress.ForgetRoomCompleted(room.SaveKey);
        }

        GameManager.SavePlayer();
        Debug.Log("전시실 시험 : 완성 기록을 지웠습니다.");
    }

    [MenuItem(CLEAR_NEXT_MENU, true)]
    [MenuItem(CLEAR_ALL_MENU, true)]
    [MenuItem(FORGET_MENU, true)]
    private static bool CanUse()
    {
        return Application.isPlaying && null != GameManager.Instance && null != GameManager.Player;
    }
}
