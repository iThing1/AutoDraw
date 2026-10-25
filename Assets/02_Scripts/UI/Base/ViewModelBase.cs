using System;

// ViewModel의 공통 부모. 값이 바뀌면 속성 이름 하나로 알리고, View는 이름을 보고 필요한 부분만 다시 그린다. 한 번 띄우고 끝나는 사건은 하위 클래스가 따로 이벤트로 낸다.
public abstract class ViewModelBase
{
    public event Action<string> OnPropertyChanged;

    public abstract void UnBind();

    protected void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged?.Invoke(propertyName);
    }
}
