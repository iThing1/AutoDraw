using UnityEngine;

public static class AppQuitter
{
    // 에디터에서는 플레이 모드만 끝낸다.
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
