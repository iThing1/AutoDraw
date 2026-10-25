using System.Collections.Generic;

// 공격하는 쪽이 최근에 쓴 되감을 수 있는 패턴의 기록. 에셋(SO)에 두면 다른 유닛과 섞여, 공격하는 쪽마다 하나씩 만든다.
// 가득 차면 가장 오래된 기록부터 밀려난다.
public class RewindLog
{
    private const int CAPACITY = 4;

    private readonly List<RewindRecord> _records = new(CAPACITY);

    public int Count { get { return _records.Count; } }

    public void Add(RewindRecord record)
    {
        if (_records.Count >= CAPACITY)
        {
            _records.RemoveAt(0);
        }

        _records.Add(record);
    }

    // 최근 기록부터 count개를 results에 담고 기록에서 지워, 같은 기록을 두 번 되감지 않게 한다.
    public void TakeLatest(int count, List<RewindRecord> results)
    {
        int takeCount = count < _records.Count ? count : _records.Count;

        for (int i = 0; i < takeCount; i++)
        {
            int last = _records.Count - 1;

            results.Add(_records[last]);
            _records.RemoveAt(last);
        }
    }
}
