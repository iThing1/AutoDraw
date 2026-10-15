using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 맵(전투 스테이지)을 만들고 지우며, 맵을 바꿀 때 화면을 가리고 지난 맵이 남긴 공용 상태를 정리한다.
public class MapSpawner : MonoBehaviour
{
    [SerializeField] private BattleManager _battleStagePrefab;

    private BattleManager _battle;

    // 공용 매니저(UI·풀·시간 배율)를 쓰므로 GameManager가 그것들을 준비한 뒤 부른다.
    public void Init()
    {
        // 첫 화면도 다시 하기와 같이 어둠에서 밝아지며 시작한다.
        ScreenFader fader = GameManager.UI.Cinematic.ScreenFader;
        fader.ShowImmediately();

        SpawnBattleStage();

        fader.FadeInAsync(destroyCancellationToken).Forget();
    }

    private void SpawnBattleStage()
    {
        if (null == _battleStagePrefab)
        {
            Debug.LogError("맵 스포너 : BattleStage 프리팹이 지정되지 않았습니다.");
            return;
        }

        _battle = Instantiate(_battleStagePrefab);
        _battle.name = _battleStagePrefab.name;
        _battle.OnResultChosen += HandleResultChosen;
    }

    // 갤러리는 아직 없어 버튼을 꺼 두었다.
    private void HandleResultChosen(ResultChoice choice)
    {
        if (ResultChoice.Retry == choice)
        {
            RestartStageAsync(destroyCancellationToken).Forget();
        }
    }

    // 스테이지를 통째로 새로 만들어 지난 판의 상태가 남지 않게 한다.
    private async UniTaskVoid RestartStageAsync(CancellationToken token)
    {
        ScreenFader fader = GameManager.UI.Cinematic.ScreenFader;

        await fader.FadeOutAsync(token);

        _battle.OnResultChosen -= HandleResultChosen;
        Destroy(_battle.gameObject);
        _battle = null;

        // Destroy는 프레임 끝에 처리되어, 이전 스테이지가 정리된 뒤에 새로 만든다.
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        ResetSharedState();
        SpawnBattleStage();

        await fader.FadeInAsync(token);
    }

    // 지난 맵의 연출이 걸어 둔 히트 스톱·슬로 모션, 풀에 남은 파괴된 오브젝트, 내려온 레터박스를 걷어 낸다.
    private void ResetSharedState()
    {
        GameManager.TimeScale.ResetToNormal();
        GameManager.Pool.RemoveDestroyed();
        GameManager.UI.Cinematic.Letterbox.HideImmediately();
    }
}
