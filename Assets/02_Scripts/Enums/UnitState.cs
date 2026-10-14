// 누가 유닛의 위치를 움직이는지. 한 번에 한 상태만 가져 이동과 패턴이 겹치지 않는다.
public enum UnitState
{
    Idle,
    Move,
    Action,
    Dead,
}
