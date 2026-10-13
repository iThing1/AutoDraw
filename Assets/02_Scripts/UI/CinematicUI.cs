using UnityEngine;

public class CinematicUI : MonoBehaviour
{
    [SerializeField] private Letterbox _letterbox;
    [SerializeField] private BossBanner _bossBanner;

    public Letterbox Letterbox { get { return _letterbox; } }
    public BossBanner BossBanner { get { return _bossBanner; } }
}
