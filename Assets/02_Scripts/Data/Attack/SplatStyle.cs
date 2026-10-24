using UnityEngine;

// 물감 자국의 모양. 개수·크기·퍼짐은 일반·치명타마다 달라서 타격 프리셋에 남긴다.
[CreateAssetMenu(fileName = "SplatStyle", menuName = "Scriptable Objects/SplatStyle")]
public class SplatStyle : ScriptableObject
{
    [SerializeField] private Sprite[] _sprites;
    [SerializeField] private bool _randomRotation = true;

    public bool HasSprites { get { return null != _sprites && 0 < _sprites.Length; } }

    public Sprite PickSprite()
    {
        return _sprites[Random.Range(0, _sprites.Length)];
    }

    public Quaternion PickRotation()
    {
        if (false == _randomRotation)
        {
            return Quaternion.identity;
        }

        return Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }
}
