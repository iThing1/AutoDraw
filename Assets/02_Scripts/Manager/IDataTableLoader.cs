using Cysharp.Threading.Tasks;
using System.Threading;

// 데이터 표 JSON을 어디서 읽을지(Resources·어드레서블 등) 정한다. 표 이름을 경로나 주소로 바꾸는 것도 로더가 한다.
// 읽은 에셋은 글만 꺼내고 바로 놓는다. 표가 없으면 null을 돌려준다.
public interface IDataTableLoader
{
    UniTask<string> LoadTextAsync(string tableName, CancellationToken token);
}
