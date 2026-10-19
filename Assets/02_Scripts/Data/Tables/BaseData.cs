using System;
using UnityEngine;

// 데이터 표 한 줄의 공통 부분. 시트 3줄의 필드 이름이 가져오기 도구에서 private 필드 이름으로 바뀐다(Id → _id, MaxHp → _maxHp).
[Serializable]
public class BaseData
{
    [SerializeField] private string _id;

    public string Id { get { return _id; } }
}
