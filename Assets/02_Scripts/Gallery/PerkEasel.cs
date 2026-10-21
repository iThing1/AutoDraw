using UnityEngine;

// 갤러리의 이젤. 바라보고 상호작용하면 특성 트리 화면이 열린다.
public class PerkEasel : GalleryInteractable
{
    [SerializeField] private string _label = "PERKS";

    public override string Label { get { return _label; } }

    public override void Interact(GalleryStage gallery)
    {
        gallery.OpenPerkTree();
    }
}
