using System.Collections.Generic;

// 표 하나만 봐서는 알 수 없는 규칙(다른 표의 Id를 가리키는지 등)을 검사한다. 어긴 곳은 errors에 더한다.
public interface IDataSheetRule
{
    void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors);
}
