// --- START OF FILE MoreTool.cs ---

using System.Text;
using System.Text.RegularExpressions;

namespace MOD_kqAfiU
{
    public static class MoreTool
    {
        /// <summary>
        /// 对单段对话文本应用富文本颜色格式化。
        /// </summary>
        /// <param name="text">需要处理的原始文本。</param>
        /// <returns>带有Unity富文本颜色标签的字符串。</returns>
        public static string ApplyColorFormatting(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            // 【修改点】更新为对比度更高的新配色方案
            string defaultColor = "#2C3E50"; // 深蓝色 (正文)
            string quoteColor = "#6A3B85";   // 深紫色 (引号)
            string bracketColor = "#A13D63"; // 深粉色 (括号)

            // 【修改点】扩展正则表达式以支持更多类型的引号和括号
            var regex = new Regex("(\".*?\")|(“.*?”)|(『.*?』)|(「.*?」)|(\\(.*?\\))|(（.*?）)|(\\[.*?\\])|(【.*?】)");

            var sb = new StringBuilder();
            int lastIndex = 0;

            foreach (Match match in regex.Matches(text))
            {
                // 1. 添加从上一个匹配结束到当前匹配开始之间的“正文”部分
                if (match.Index > lastIndex)
                {
                    string plainText = text.Substring(lastIndex, match.Index - lastIndex);
                    sb.Append($"<color={defaultColor}>{plainText}</color>");
                }

                // 2. 根据匹配到的内容类型，添加带相应颜色的部分
                string content = match.Value;
                // 【修改点】更新if判断条件以匹配新的引号类型
                if (content.StartsWith("\"") || content.StartsWith("“") || content.StartsWith("『") || content.StartsWith("「"))
                {
                    // 这是引号内容
                    sb.Append($"<color={quoteColor}>{content}</color>");
                }
                // 【修改点】更新else if判断条件以匹配新的括号类型
                else if (content.StartsWith("(") || content.StartsWith("（") || content.StartsWith("[") || content.StartsWith("【"))
                {
                    // 这是括号内容
                    sb.Append($"<color={bracketColor}>{content}</color>");
                }

                // 3. 更新下一个“正文”部分的起始索引
                lastIndex = match.Index + match.Length;
            }

            // 4. 添加最后一个匹配之后剩余的“正文”部分
            if (lastIndex < text.Length)
            {
                string remainingText = text.Substring(lastIndex);
                sb.Append($"<color={defaultColor}>{remainingText}</color>");
            }

            return sb.ToString();
        }
    }
}
// --- END OF FILE MoreTool.cs ---