// 화면 조작과 HUD 상태 변화에 붙는 소리의 종류. 어떤 소리를 낼지는 UISoundSettings가 정한다.
public enum UISoundKind
{
    // 소리를 내지 않는다.
    None,
    Click,
    Open,
    Close,
    Confirm,
    Equip,
    Unlock,
    // 잠긴 것을 고르거나, 쓸 수 없을 때 눌렀다.
    Error,
    WeaponSwitch,
    SkillReady,
}
