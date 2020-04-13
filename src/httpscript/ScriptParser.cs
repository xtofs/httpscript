using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace xtofs.httpscript
{
    internal class ScriptParser
    {
        public ScriptParser()
        {
        }

        public Script Load(string path)
        {
            using var reader = File.OpenText(path);
            return Load(Path.GetFileName(path), reader);
        }

        public Script Load(string name, TextReader reader)
        {
            return Load(name, new LineReader(reader));
        }

        public Script Load(string name, LineReader reader)
        {
            var instructions = new List<Statement>();
            while (!reader.IsEof)
            {
                if (reader.Current.StartsWith("#!"))
                {
                    instructions.Add(ParseDirective(reader));
                }
                else if (reader.Current.StartsWith("#"))
                {
                    instructions.Add(ParseComment(reader));
                }
                else if (reader.Current.StartsWith("@"))
                {
                    instructions.Add(ParseExtract(reader));
                }
                else
                {
                    instructions.Add(ParseRequest(reader));
                }
            }
            return new Script(name, instructions);
        }


        private Statement ParseRequest(LineReader reader)
        {
            var request = string.Empty;
            var headers = new List<string>();
            var body = string.Empty;
            var state = 0;
            while (!reader.IsEof && !reader.Current.StartsWith("#") && !reader.Current.StartsWith("@"))
            {
                var line = reader.Current;
                if (state == 0)
                {
                    // skiping empty lines at start of request
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        reader.MoveNext();
                        continue;
                    }
                    request = line;
                    state = 1;
                    reader.MoveNext();
                }
                else if (state == 1)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        state = 2;
                    }
                    else
                    {
                        headers.Add(line);
                    }
                    reader.MoveNext();
                }
                else if (state == 2)
                {
                    var list = new List<string>();
                    while (!reader.IsEof && !reader.Current.StartsWith("#"))
                    {
                        list.Add(reader.Current);
                        reader.MoveNext();
                    }
                    body = string.Join("\r\n", list);
                    state = 3;
                    break;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return new RequestStatement(request, headers, body);
        }

        private Statement ParseExtract(LineReader reader)
        {

            var line = reader.Current;
            var match = Regex.Match(line, "^@\\s*([a-z][a-z0-9]*)\\s*=\\s*(.+)$");
            if (match.Success)
            {
                reader.MoveNext();
                var name = match.Groups[1].Value;
                var expr = match.Groups[2].Value;
                return new ExtractStatement(name, expr);
            }
            else
            {
                throw new ArgumentException("not an assignment");
            }
        }

        private Statement ParseComment(LineReader reader)
        {
            var lines = new List<string>();
            var first = true;
            while (!reader.IsEof && reader.Current.StartsWith("#") && !reader.Current.StartsWith("#!"))
            {
                var line = reader.Current;
                line = line.StripPrefix("# ").StripPrefix("#");
                if (!first || !string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                    first = false;
                }
                reader.MoveNext();
            }
            return new CommentStatement(lines);
        }

        private Statement ParseDirective(LineReader reader)
        {
            var line = reader.Current;
            if (!line.StartsWith("#!")) { throw new ArgumentException("Directives must start with #!"); }
            line = line.Substring(2).Trim();
            if (line.StartsWith(HostStatement.Identifier, StringComparison.InvariantCultureIgnoreCase))
            {
                reader.MoveNext();
                return new HostStatement(line.StripPrefix(HostStatement.Identifier, StringComparison.InvariantCultureIgnoreCase).Trim());
            }
            if (line.StartsWith(LimitStatement.Identifier, StringComparison.InvariantCultureIgnoreCase))
            {
                var content = line.StripPrefix(LimitStatement.Identifier, StringComparison.InvariantCultureIgnoreCase).Trim();
                if (Int32.TryParse(content, out int value))
                {
                    reader.MoveNext();
                    return new LimitStatement(value);
                }
                throw new Exception($"Limit statement with non numeric value {line}");
            }
            if (line.StartsWith(ExtractStatement.Identifier, StringComparison.InvariantCultureIgnoreCase))
            {
                var content = line.StripPrefix(ExtractStatement.Identifier, StringComparison.InvariantCultureIgnoreCase).Trim();
                var parts = content.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                reader.MoveNext();
                return new ExtractStatement(parts[0], parts[1]);
            }
            throw new Exception($"Unknown directive {line}");
        }
    }
}