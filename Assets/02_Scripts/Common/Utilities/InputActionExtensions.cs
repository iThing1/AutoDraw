using UnityEngine.InputSystem;

public static class InputActionExtensions
{
    // HUD에 보여 줄 키 이름. 입력 에셋에서 첫 번째로 연결한 키(키보드)를 쓰므로, 키를 바꾸면 따라 바뀐다.
    public static string GetKeyLabel(this InputActionReference actionReference)
    {
        if (null == actionReference)
        {
            return string.Empty;
        }

        return actionReference.action.GetBindingDisplayString(0);
    }

    // 입력이 연결되지 않았으면 아무 일도 하지 않는다. 연결 누락은 쓰는 쪽이 따로 알린다.
    public static void Enable(this InputActionReference actionReference)
    {
        if (null != actionReference)
        {
            actionReference.action.Enable();
        }
    }

    public static bool WasPressedThisFrame(this InputActionReference actionReference)
    {
        return null != actionReference && actionReference.action.WasPressedThisFrame();
    }
}
