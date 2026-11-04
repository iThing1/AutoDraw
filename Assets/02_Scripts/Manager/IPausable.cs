// 공용 일시정지(PauseController)가 지금 맵(갤러리, 전투)에 묻고 알리는 것. 시간 정지와 입력 차단은 컨트롤러가 어느 맵에서나 똑같이 한다.
public interface IPausable
{
    // 지금 멈춰도 되는지. 컷신이나 맵 안의 창이 열린 동안은 false다.
    bool CanPause { get; }
    // 메뉴에 다시 하기·갤러리로 버튼을 보여 줄지.
    bool CanLeave { get; }

    // Esc가 메뉴보다 먼저 닫을 맵 안의 창이 열려 있으면 닫고 true를 돌려준다.
    bool TryCloseScreen();
    // 멈출 때와 풀 때 맵이 따로 할 일(커서, 조준점).
    void OnPauseChanged(bool isPaused);
}
