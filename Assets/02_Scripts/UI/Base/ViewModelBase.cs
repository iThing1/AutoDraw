using System;

// ViewModel의 공통 부모. 값이 바뀌면 속성 이름 하나로 알리고, View는 이름을 보고 필요한 부분만 다시 그린다.
// 게임 컴포넌트(Health 등)가 모델이라, ViewModel은 그 이벤트를 받아 이름 알림으로 바꿔 준다. 다 쓰면 UnBind로 구독을 푼다.
// 한 번 띄우고 끝나는 사건(문구, 사용 표시)은 이름 알림 대신 하위 클래스가 따로 이벤트로 낸다.
public abstract class ViewModelBase
{
    public event Action<string> OnPropertyChanged;

    public abstract void UnBind();

    protected void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged?.Invoke(propertyName);
    }
}
