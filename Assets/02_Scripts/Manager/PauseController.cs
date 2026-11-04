using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// 갤러리와 전투가 함께 쓰는 일시정지. Esc를 받아 시간을 멈추고 조작 입력을 끈 채 메뉴를 띄운다.
// 언제 멈출 수 있는지와 멈출 때 따로 할 일은 지금 맵(IPausable)이 정하고, 맵을 바꾸는 쪽(MapSpawner)이 대상을 바꿔 준다.
public class PauseController
{
    private readonly PauseMenuUI _menuPrefab;
    private readonly InputAction _pauseAction;
    // 멈출 때 끈 조작 입력. 풀 때 이것만 다시 켠다.
    private readonly List<InputAction> _blockedActions = new();

    private IPausable _target;
    private PauseMenuUI _menu;
    // 스테이지를 떠나기로 해 메뉴를 닫은 뒤에도 화면이 가려질 때까지 멈춰 있다.
    private bool _isPaused;

    // 메뉴에서 다시 하기·갤러리로를 골랐다. 멈춘 채로 알리고, 맵을 바꾸며 대상을 바꿀 때 푼다.
    public event Action<ResultChoice> OnLeaveChosen;

    public PauseController(PauseMenuUI menuPrefab, InputAction pauseAction)
    {
        _menuPrefab = menuPrefab;
        _pauseAction = pauseAction;

        _pauseAction.Enable();
    }

    // 맵을 바꿀 때 부른다. 비워 두면(타이틀, 맵 이동 중) Esc를 무시한다.
    public void SetTarget(IPausable target)
    {
        Release();
        _target = target;
    }

    // Esc는 설정 창, 맵 안의 창, 메뉴 순으로 닫고, 닫을 것이 없을 때 멈춘다.
    public void Tick()
    {
        if (null == _target || false == _pauseAction.WasPressedThisFrame())
        {
            return;
        }

        if (null != _menu)
        {
            if (false == _menu.TryCloseSettings())
            {
                Resume();
            }

            return;
        }

        // 떠나기로 해 메뉴를 닫은 뒤라 화면이 바뀔 때까지 기다린다.
        if (_isPaused)
        {
            return;
        }

        if (false == _target.TryCloseScreen() && _target.CanPause)
        {
            Pause();
        }
    }

    private void Pause()
    {
        _menu = GameManager.UI.Open(_menuPrefab, UILayer.Overlay);

        if (null == _menu)
        {
            return;
        }

        if (_target.CanLeave)
        {
            _menu.ShowStageButtons();
        }

        _menu.OnResume += Resume;
        _menu.OnLeaveChosen += HandleLeaveChosen;

        _isPaused = true;
        GameManager.TimeScale.SetPaused(true);
        BlockInput();
        _target.OnPauseChanged(true);

        GameManager.Sound.PlayUi(UISoundKind.Open);
    }

    private void Resume()
    {
        GameManager.Sound.PlayUi(UISoundKind.Close);
        CloseMenu();
        UnblockInput();

        _isPaused = false;
        GameManager.TimeScale.SetPaused(false);
        _target.OnPauseChanged(false);
    }

    // 떠나는 맵은 곧 사라지거나 숨으므로 맵에는 알리지 않고, 끈 입력과 시간만 되돌린다.
    private void Release()
    {
        CloseMenu();
        UnblockInput();

        if (_isPaused)
        {
            _isPaused = false;
            GameManager.TimeScale.SetPaused(false);
        }
    }

    private void HandleLeaveChosen(ResultChoice choice)
    {
        GameManager.Sound.PlayUi(UISoundKind.Confirm);
        CloseMenu();

        OnLeaveChosen?.Invoke(choice);
    }

    private void CloseMenu()
    {
        if (null == _menu)
        {
            return;
        }

        _menu.OnResume -= Resume;
        _menu.OnLeaveChosen -= HandleLeaveChosen;
        _menu.TryCloseSettings();
        GameManager.UI.Close(_menu);
        _menu = null;
    }

    // 시간이 멈춰도 Update는 돌아 무기 고르기·스킬·시점 같은 입력이 먹히므로, 일시정지 키만 남기고 같은 묶음에서 켜진 입력을 끈다.
    private void BlockInput()
    {
        ReadOnlyArray<InputAction> actions = _pauseAction.actionMap.actions;

        for (int i = 0; i < actions.Count; i++)
        {
            InputAction action = actions[i];

            if (action != _pauseAction && action.enabled)
            {
                action.Disable();
                _blockedActions.Add(action);
            }
        }
    }

    private void UnblockInput()
    {
        for (int i = 0; i < _blockedActions.Count; i++)
        {
            _blockedActions[i].Enable();
        }

        _blockedActions.Clear();
    }
}
