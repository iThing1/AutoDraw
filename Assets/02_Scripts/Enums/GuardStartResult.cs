// 막기 키를 눌렀을 때 막기를 쓸 수 있는지. 안 되면 왜 안 되는지 함께 알려, 화면이 이유에 맞는 표시를 고르게 한다.
public enum GuardStartResult
{
    Ready,
    OnCooldown,
    OutOfGauge,
}
