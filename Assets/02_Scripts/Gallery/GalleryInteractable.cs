using UnityEngine;

// 갤러리에서 바라보고 상호작용 키로 고르는 물건. 콜라이더와 같은 오브젝트에 둔다.
public abstract class GalleryInteractable : MonoBehaviour
{
    // HUD 안내에 "[키] 이 문구"로 보인다.
    [SerializeField] private string _actionLabel = "ENTER";

    public string ActionLabel { get { return _actionLabel; } }
    public abstract string Label { get; }
    public virtual bool IsLocked { get { return false; } }

    public abstract void SetHighlighted(bool isHighlighted);
}
