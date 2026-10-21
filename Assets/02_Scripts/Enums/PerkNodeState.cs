// 특성 트리 노드 하나의 지금 상태. 저장하지 않고 찍은 특성과 남은 포인트로 매번 구한다.
public enum PerkNodeState
{
    // 부모 노드를 아직 다 찍지 않았다.
    Locked,
    // 부모는 다 찍었지만 포인트가 모자란다.
    Unaffordable,
    Available,
    Unlocked,
}
