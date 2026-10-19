using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 맵(갤러리·전투 스테이지)을 만들고 지우며, 맵을 바꿀 때 화면을 가리고 지난 맵이 남긴 공용 상태를 정리한다.
// 갤러리는 한 번 만들어 두고 숨겼다 다시 보이고, 전투 스테이지는 들어갈 때마다 새로 만든다.
public class MapSpawner : MonoBehaviour
{
    [SerializeField] private GalleryStage _galleryPrefab;

    [Header("테스트")]
    // 켜면 갤러리를 건너뛰고 아래 스테이지로 바로 시작한다. 전투만 고칠 때 쓴다.
    [SerializeField] private bool _startInStage;
    [SerializeField] private StageData _testStage;

    private GalleryStage _gallery;
    private BattleManager _battle;
    private StageData _currentStage;
    // 맵을 바꾸는 동안 들어온 입력(액자 선택·결과 버튼)이 겹쳐 이동을 두 번 시작하지 않게 막는다.
    private bool _isMoving;

    // 공용 매니저(UI·풀·시간 배율)를 쓰므로 GameManager가 그것들을 준비한 뒤 부른다.
    public void Init()
    {
        if (null == _galleryPrefab)
        {
            Debug.LogError("맵 스포너 : 갤러리 프리팹이 지정되지 않았습니다.");
            return;
        }

        ScreenFader fader = GameManager.UI.Cinematic.ScreenFader;
        fader.ShowImmediately();

        _gallery = Instantiate(_galleryPrefab);
        _gallery.name = _galleryPrefab.name;
        _gallery.OnStageChosen += HandleStageChosen;

        if (_startInStage && null != _testStage)
        {
            HideGallery();
            SpawnStage(_testStage);
        }
        else
        {
            ShowGallery();
        }

        fader.FadeInAsync(destroyCancellationToken).Forget();
    }

    private void HandleStageChosen(StageData stage)
    {
        if (false == _isMoving)
        {
            EnterStageAsync(stage, destroyCancellationToken).Forget();
        }
    }

    private void HandleResultChosen(ResultChoice choice)
    {
        if (_isMoving)
        {
            return;
        }

        if (ResultChoice.Retry == choice)
        {
            RestartStageAsync(destroyCancellationToken).Forget();
        }
        else
        {
            ReturnToGalleryAsync(destroyCancellationToken).Forget();
        }
    }

    // 갤러리로 돌아가면 이 그림이 액자에 걸린다. 텍스처는 파일로 남겼으니 바로 지운다.
    private void HandleStageCleared(Texture2D painting)
    {
        if (null == painting)
        {
            return;
        }

        PaintingStorage.Save(_currentStage, painting);
        Destroy(painting);
        _gallery.MarkPaintingChanged(_currentStage);
    }

    private async UniTaskVoid EnterStageAsync(StageData stage, CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);

        HideGallery();
        SpawnStage(stage);

        await FinishMoveAsync(token);
    }

    // 스테이지를 통째로 새로 만들어 지난 판의 상태가 남지 않게 한다.
    private async UniTaskVoid RestartStageAsync(CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);
        await DestroyStageAsync(token);

        SpawnStage(_currentStage);

        await FinishMoveAsync(token);
    }

    private async UniTaskVoid ReturnToGalleryAsync(CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);
        await DestroyStageAsync(token);

        ShowGallery();

        await FinishMoveAsync(token);
    }

    private async UniTask FinishMoveAsync(CancellationToken token)
    {
        await GameManager.UI.Cinematic.ScreenFader.FadeInAsync(token);

        _isMoving = false;
    }

    private void SpawnStage(StageData stage)
    {
        if (null == stage || null == stage.StagePrefab)
        {
            Debug.LogError("맵 스포너 : 들어갈 스테이지나 그 전투 스테이지 프리팹이 비어 있습니다.");
            return;
        }

        _currentStage = stage;

        _battle = Instantiate(stage.StagePrefab);
        _battle.name = stage.StagePrefab.name;
        _battle.OnResultChosen += HandleResultChosen;
        _battle.OnStageCleared += HandleStageCleared;
    }

    // Destroy는 프레임 끝에 처리되어, 이전 스테이지가 카메라 경계와 UI를 정리한 뒤에 다음 맵을 꺼낸다.
    private async UniTask DestroyStageAsync(CancellationToken token)
    {
        _battle.OnResultChosen -= HandleResultChosen;
        _battle.OnStageCleared -= HandleStageCleared;
        Destroy(_battle.gameObject);
        _battle = null;

        await UniTask.Yield(PlayerLoopTiming.Update, token);

        ResetSharedState();
    }

    // 갤러리는 자기 카메라로 그리므로 전투 카메라를 끄고, 전투로 들어갈 때 다시 켠다.
    private void ShowGallery()
    {
        GameManager.CameraRig.SetOutputEnabled(false);
        _gallery.Show();
    }

    private void HideGallery()
    {
        _gallery.Hide();
        GameManager.CameraRig.SetOutputEnabled(true);
    }

    // 지난 맵의 연출이 걸어 둔 히트 스톱·슬로 모션, 풀에 남은 파괴된 오브젝트, 내려온 레터박스를 걷어 낸다. 느려지는 구역은 꺼질 때 스스로 빠진다.
    private void ResetSharedState()
    {
        GameManager.TimeScale.ResetToNormal();
        GameManager.Pool.RemoveDestroyed();
        GameManager.UI.Cinematic.Letterbox.HideImmediately();
    }
}
