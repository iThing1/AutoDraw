using UnityEngine;

// 갤러리에 숨긴 팔레트. 하나로 특성을 한 번 다시 찍는다.
public class PaletteItem : GalleryInteractable
{
    [SerializeField] private string _label = "PALETTE";
    // 진행 기록에 남으므로 겹치지 않게 하고, 정한 뒤에는 바꾸지 않는다.
    [SerializeField] private string _paletteId;

    public string PaletteId { get { return _paletteId; } }
    public override string Label { get { return _label; } }

    public override void Interact(GalleryStage gallery)
    {
        gallery.CollectPalette(this);
    }
}
