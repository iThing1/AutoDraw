using System;
using UnityEngine;
using UnityEngine.UI;

// 일시정지 메뉴. 계속하기는 메뉴를 연 쪽이 닫고, 종료는 여기서 바로 처리한다.
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;

    public event Action OnResume;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(HandleResumeClicked);
        _quitButton.onClick.AddListener(HandleQuitClicked);
    }

    private void HandleResumeClicked()
    {
        OnResume?.Invoke();
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
