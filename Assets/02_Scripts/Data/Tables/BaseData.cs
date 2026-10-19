using System;
using UnityEngine;

// 필드 이름은 시트 이름 앞에 _를 붙이고 첫 글자를 소문자로 한다(MaxHp → _maxHp).
[Serializable]
public class BaseData
{
    [SerializeField] private string _id;

    public string Id { get { return _id; } }
}
