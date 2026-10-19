using System.Collections.Generic;
using UnityEngine;

// 다 읽은 데이터 표 하나. 만든 뒤에는 바뀌지 않고 Id로 찾거나 전체를 돈다.
public class DataTable<T> where T : BaseData
{
    private readonly string _name;
    private readonly Dictionary<string, T> _rows = new();
    private readonly List<T> _all = new();

    public string Name { get { return _name; } }
    // 시트에 적힌 순서 그대로다.
    public IReadOnlyList<T> All { get { return _all; } }

    // rows가 null이면 빈 표가 된다. 겹치는 Id는 가져오기 도구가 막지만, 손으로 고친 JSON에 대비해 한 번 더 거른다.
    public DataTable(string name, T[] rows)
    {
        _name = name;

        if (null == rows)
        {
            return;
        }

        foreach (T row in rows)
        {
            if (null == row || string.IsNullOrEmpty(row.Id))
            {
                Debug.LogError($"데이터 표 : {name}에 Id가 빈 줄이 있습니다.");
                continue;
            }

            if (false == _rows.TryAdd(row.Id, row))
            {
                Debug.LogError($"데이터 표 : {name}에 Id {row.Id}이(가) 겹칩니다.");
                continue;
            }

            _all.Add(row);
        }
    }

    // 꼭 있어야 하는 줄을 찾을 때 쓴다. 없으면 오류를 남기고 null이다.
    public T Get(string id)
    {
        if (TryGet(id, out T row))
        {
            return row;
        }

        Debug.LogError($"데이터 표 : {_name}에 Id {id}이(가) 없습니다.");
        return null;
    }

    public bool TryGet(string id, out T row)
    {
        if (null == id)
        {
            row = null;
            return false;
        }

        return _rows.TryGetValue(id, out row);
    }
}
