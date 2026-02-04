using System.Text;

namespace Mcp.Common;

public static class TagFormatter
{
    public static string FormatConfirmationMessage(IList<string> tagsList, string baseMessage)
    {
        // Calculate initial capacity to avoid resizing.
        // Base message length
        // + 2 chars for "\n\n" if tags exist
        // + for each tag:
        //   + 1 char for '\n' separator (except first)
        //   + 3 chars for "- \"" prefix
        //   + 1 char for "\"" suffix
        //   + tag.Length
        //   + heuristic for escaping: assume 10% extra length for escaped quotes (safe overestimation is better than resizing)
        int capacity = baseMessage.Length;
        if (tagsList.Count > 0)
        {
            capacity += 2; // "\n\n"
            foreach (var t in tagsList)
            {
                capacity += 5 + t.Length + (t.Length / 10); // 5 = "- \"" + "\"" + potential '\n' (averaged/simplified)
            }
        }

        var sb = new StringBuilder(baseMessage, capacity);

        if (tagsList.Count > 0)
        {
            sb.Append("\n\n");
            for (int i = 0; i < tagsList.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }
                sb.Append("- \"");

                var tag = tagsList[i];
                for (int j = 0; j < tag.Length; j++)
                {
                    var c = tag[j];
                    if (c == '"')
                    {
                        sb.Append('\\');
                        sb.Append('"');
                    }
                    else if (c == '\n')
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                sb.Append('"');
            }
        }

        return sb.ToString();
    }
}
