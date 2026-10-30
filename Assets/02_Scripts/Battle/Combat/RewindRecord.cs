using Cysharp.Threading.Tasks;
using System.Threading;

// 패턴 하나를 쏜 자리·모양의 기록. 되감기가 이 기록을 거꾸로 다시 재생한다.
// 거꾸로 재생하는 방법은 패턴마다 다르므로, 기록을 남기는 패턴이 하위 클래스를 만들어 자기 재생 방법을 부른다.
// 되감은 패턴은 tint를 예고·먹 색에 덧입혀 원래 패턴과 구분해 보이게 한다.
public abstract class RewindRecord
{
    public abstract UniTask ReplayAsync(AttackContext context, InkTint tint, float gaugeGain, CancellationToken token);
}
