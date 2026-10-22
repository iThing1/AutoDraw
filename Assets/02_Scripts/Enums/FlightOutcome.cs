public enum FlightOutcome
{
    Canceled,
    // 피해는 도착한 쪽에서 다시 판정한다.
    Arrived,
    Blocked,
    Missed,
    // 피하면 멈추도록 한 비행만 낸다(HomingFlight.EndsOnDodge).
    Dodged,
}
