using System;
using System.Collections.Generic;

namespace FunctionGraphOverview
{
    internal static class LanguageMap
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            { ".c", "C" },
            { ".cpp", "C++" },
            { ".cxx", "C++" },
            { ".cc", "C++" },
            { ".h", "C++" },
            { ".hpp", "C++" },
            { ".go", "Go" },
            { ".java", "Java" },
            { ".py", "Python" },
            { ".ts", "TypeScript" },
            { ".tsx", "TSX" },
            { ".cs", "C#" },
        };

        public static bool TryGetLanguage(string extension, out string language)
        {
            return Map.TryGetValue(extension, out language);
        }

        private static readonly Dictionary<string, string> ContentTypeMap = new Dictionary<
            string,
            string
        >(StringComparer.OrdinalIgnoreCase)
        {
            { "C/C++", "C++" },
            { "Python", "Python" },
            { "TypeScript", "TypeScript" },
            { "Java", "Java" },
            { "Go", "Go" },
            { "CSharp", "C#" },
        };

        public static bool TryGetLanguageFromContentType(
            string contentTypeName,
            out string language
        )
        {
            return ContentTypeMap.TryGetValue(contentTypeName, out language);
        }
    }
}
