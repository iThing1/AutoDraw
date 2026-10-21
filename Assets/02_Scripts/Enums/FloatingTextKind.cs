// HUD에 잠깐 떠오르는 문구의 종류. 문구와 색은 화면 설정(FloatingTextSettings)이 정한다.
public enum FloatingTextKind
{
    Guard,
    NoGauge,
    GimmickClear,
    GimmickFail,
    // 문구에 회복량 자리({0})가 있다.
    Heal,
}
