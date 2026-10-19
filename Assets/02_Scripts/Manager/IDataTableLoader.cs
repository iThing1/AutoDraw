using Cysharp.Threading.Tasks;
using System.Threading;

// 어디서 읽을지(Resources·어드레서블)와 표 이름을 경로·주소로 바꾸는 일은 로더가 맡는다. 표가 없으면 null.
public interface IDataTableLoader
{
    UniTask<string> LoadTextAsync(string tableName, CancellationToken token);
}
