using System.Collections.Generic;

public interface IDataSheetRule
{
    void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors);
}
