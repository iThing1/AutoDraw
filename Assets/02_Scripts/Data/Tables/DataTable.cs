using System.Collections.Generic;
using UnityEngine;

public class DataTable<T> where T : BaseData
{
    private readonly string _name;
    private readonly Dictionary<string, T> _rows = new();
    private readonly List<T> _all = new();

    public string Name { get { return _name; } }
    public IReadOnlyList<T> All { get { return _all; } }

    // 손으로 고친 JSON에 대비해 겹치는 Id를 한 번 더 거른다.
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
