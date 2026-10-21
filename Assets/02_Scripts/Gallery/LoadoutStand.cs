using UnityEngine;

// 갤러리의 장비 보관대. 바라보고 상호작용하면 장비 화면이 열린다.
public class LoadoutStand : GalleryInteractable
{
    [SerializeField] private string _label = "EQUIPMENT";

    public override string Label { get { return _label; } }
}
