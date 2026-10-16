using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 갤러리를 1인칭으로 걸어 다니며, 화면 가운데로 바라보는 액자를 찾아 상호작용 키로 고른다.
[RequireComponent(typeof(CharacterController))]
public class GalleryPlayer : MonoBehaviour
{
    [SerializeField] private Transform _camera;

    [Header("입력")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private InputActionReference _interactAction;

    [Header("이동")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    // 마우스는 프레임마다 움직인 픽셀이 들어오므로 시간을 곱하지 않고 이 배율만 곱한다.
    [SerializeField, Min(0f)] private float _lookSensitivity = 0.12f;
    [SerializeField, Range(0f, 89f)] private float _maxPitch = 80f;

    [Header("상호작용")]
    [SerializeField, Min(0f)] private float _interactDistance = 3f;

    private CharacterController _controller;
    private bool _hasInputs;
    private Vector3 _cameraHomePosition;
    private float _pitch;
    private bool _isControlEnabled;
    private GalleryFrame _focusedFrame;

    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private float _savedPitch;

    public event Action<GalleryFrame> OnFocusChanged;
    public event Action<GalleryFrame> OnInteract;

    // HUD에 보여 줄 상호작용 키 이름. 입력 에셋에서 키를 바꾸면 따라 바뀐다.
    public string InteractKeyLabel { get { return _interactAction.GetKeyLabel(); } }
    public bool IsControlEnabled { get { return _isControlEnabled; } }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _cameraHomePosition = _camera.localPosition;

        _hasInputs = null != _moveAction && null != _lookAction && null != _interactAction;

        if (false == _hasInputs)
        {
            Debug.LogError("갤러리 플레이어 : 이동·시점·상호작용 입력(InputActionReference)이 지정되지 않았습니다.");
        }
    }

    private void OnEnable()
    {
        if (false == _hasInputs)
        {
            return;
        }

        _moveAction.Enable();
        _lookAction.Enable();
        _interactAction.Enable();
    }

    private void Update()
    {
        if (false == _isControlEnabled || false == _hasInputs)
        {
            return;
        }

        Look();
        Move();
        UpdateFocus();

        if (null != _focusedFrame && _interactAction.WasPressedThisFrame())
        {
            OnInteract?.Invoke(_focusedFrame);
        }
    }

    public void SetControlEnabled(bool isEnabled)
    {
        _isControlEnabled = isEnabled;

        if (false == isEnabled)
        {
            SetFocus(null);
        }
    }

    // 액자로 다가가는 연출 전에 서 있던 자리를 기억해, 갤러리로 돌아오면 그 자리에서 다시 시작한다.
    public void SavePose()
    {
        _savedPosition = transform.position;
        _savedRotation = transform.rotation;
        _savedPitch = _pitch;
    }

    public void RestorePose()
    {
        _camera.DOKill();

        // CharacterController는 켜진 채로 위치를 바꾸면 이전 자리로 되돌려 놓으므로 잠깐 끈다.
        _controller.enabled = false;
        transform.SetPositionAndRotation(_savedPosition, _savedRotation);
        _controller.enabled = true;

        _pitch = _savedPitch;
        _camera.localPosition = _cameraHomePosition;
        _camera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // 카메라만 액자 앞 자리로 옮겨, 그림이 화면을 채우며 그 안으로 들어가는 것처럼 보이게 한다.
    public UniTask MoveCameraToAsync(Transform viewPoint, float duration, Ease ease, CancellationToken token)
    {
        _camera.DOKill();

        Sequence move = DOTween.Sequence()
            .Join(_camera.DOMove(viewPoint.position, duration))
            .Join(_camera.DORotateQuaternion(viewPoint.rotation, duration))
            .SetEase(ease);

        return move.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // 돌아올 때 그림 앞에서 시작해 서 있던 자리로 물러나도록, 카메라만 액자 앞에 먼저 둔다.
    public void PlaceCameraAt(Transform viewPoint)
    {
        _camera.DOKill();
        _camera.SetPositionAndRotation(viewPoint.position, viewPoint.rotation);
    }

    public UniTask MoveCameraHomeAsync(float duration, Ease ease, CancellationToken token)
    {
        _camera.DOKill();

        Sequence move = DOTween.Sequence()
            .Join(_camera.DOLocalMove(_cameraHomePosition, duration))
            .Join(_camera.DOLocalRotateQuaternion(Quaternion.Euler(_pitch, 0f, 0f), duration))
            .SetEase(ease);

        return move.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // 좌우는 몸을 돌리고 위아래는 카메라만 숙여, 걸어가는 방향이 고개 각도에 따라 기울지 않게 한다.
    private void Look()
    {
        Vector2 look = _lookAction.action.ReadValue<Vector2>() * _lookSensitivity;

        transform.Rotate(0f, look.x, 0f);

        _pitch = Mathf.Clamp(_pitch - look.y, -_maxPitch, _maxPitch);
        _camera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f);
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        // SimpleMove는 초당 속도를 받아 중력까지 알아서 더해 준다.
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 액자 콜라이더는 액자 컴포넌트와 같은 오브젝트에 있어, 맞은 오브젝트만 확인한다.
    private void UpdateFocus()
    {
        GalleryFrame frame = null;

        if (Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, _interactDistance))
        {
            hit.collider.TryGetComponent(out frame);
        }

        SetFocus(frame);
    }

    // 바라보던 액자의 강조를 끄고 새로 바라보는 액자를 강조한다.
    private void SetFocus(GalleryFrame frame)
    {
        if (frame == _focusedFrame)
        {
            return;
        }

        if (null != _focusedFrame)
        {
            _focusedFrame.SetHighlighted(false);
        }

        _focusedFrame = frame;

        if (null != frame)
        {
            frame.SetHighlighted(true);
        }

        OnFocusChanged?.Invoke(frame);
    }
}
