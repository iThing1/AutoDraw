using System.Collections.Generic;
using System.Text;

// CSV 글을 줄·칸으로 나눈다. 따옴표로 감싼 칸 안의 쉼표·줄바꿈·겹따옴표("")도 한 칸으로 읽는다.
public static class CsvReader
{
    public static List<string[]> Read(string text)
    {
        List<string[]> rows = new();
        List<string> cells = new();
        StringBuilder cell = new();
        bool isQuoted = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (isQuoted)
            {
                if ('"' != c)
                {
                    cell.Append(c);
                }
                else if (i + 1 < text.Length && '"' == text[i + 1])
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    isQuoted = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    isQuoted = true;
                    break;

                case ',':
                    EndCell();
                    break;

                case '\r':
                    break;

                case '\n':
                    EndRow();
                    break;

                default:
                    cell.Append(c);
                    break;
            }
        }

        // 마지막 줄 끝에 줄바꿈이 없을 때.
        if (0 < cell.Length || 0 < cells.Count)
        {
            EndRow();
        }

        return rows;

        void EndCell()
        {
            cells.Add(cell.ToString());
            cell.Clear();
        }

        void EndRow()
        {
            EndCell();
            rows.Add(cells.ToArray());
            cells.Clear();
        }
    }
}
