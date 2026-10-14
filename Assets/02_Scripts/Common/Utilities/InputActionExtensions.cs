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

    public static bool WasPressedThisFrame(this InputActionReference actionReference)
    {
        return null != actionReference && actionReference.action.WasPressedThisFrame();
    }
}
