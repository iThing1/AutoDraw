using UnityEngine;

// ViewModel을 받아 그리기만 하는 화면 부품의 공통 틀. 이름 알림 구독·해제는 여기서 하고, 하위 View는 무엇을 다시 그릴지만 정한다.
public abstract class ViewBase<TViewModel> : MonoBehaviour where TViewModel : ViewModelBase
{
    private TViewModel _viewModel;

    protected TViewModel ViewModel { get { return _viewModel; } }

    // 같은 View에 다른 ViewModel을 다시 꽂을 수 있게, 이전 구독은 먼저 푼다.
    public void Bind(TViewModel viewModel)
    {
        UnBind();

        if (null == viewModel)
        {
            return;
        }

        _viewModel = viewModel;
        _viewModel.OnPropertyChanged += OnPropertyChanged;

        OnBind();
        RefreshAll();
    }

    // ViewModel의 구독은 풀지 않는다. ViewModel을 만든 쪽이 따로 UnBind한다.
    public void UnBind()
    {
        if (null == _viewModel)
        {
            return;
        }

        OnUnbind();

        _viewModel.OnPropertyChanged -= OnPropertyChanged;
        _viewModel = null;
    }

    protected virtual void OnDestroy()
    {
        UnBind();
    }

    protected virtual void OnBind()
    {
    }

    protected virtual void OnUnbind()
    {
    }

    protected abstract void OnPropertyChanged(string propertyName);

    protected abstract void RefreshAll();
}
