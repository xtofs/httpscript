using System;
using System.IO;

namespace xtofs.httpscript
{
    public static class StringExtensions
    {
        public static string StripPrefix(this string source, string prefix, StringComparison comparison = StringComparison.InvariantCulture) =>
            source.StartsWith(prefix, comparison) ? source.Substring(prefix.Length) : source;

        public static bool TryReadLine(this TextReader reader, out string line)
        {
            line = reader.ReadLine();
            return line != null;
        }
    }
}