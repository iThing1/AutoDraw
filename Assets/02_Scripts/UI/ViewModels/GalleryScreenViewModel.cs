using System;

// 갤러리에서 물건과 상호작용해 여는 창(장비, 특성 트리)의 공통 부모. 닫기를 누르면 알리고, 닫는 것은 연 쪽(GalleryStage)이 한다.
public abstract class GalleryScreenViewModel : ViewModelBase
{
    public event Action OnCloseRequested;

    public void RequestClose()
    {
        OnCloseRequested?.Invoke();
    }
}
