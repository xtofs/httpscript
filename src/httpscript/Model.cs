using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace xtofs.httpscript
{
    public class Script
    {
        public Script(string fileName, IEnumerable<Statement> statements)
        {
            FileName = fileName;
            Statements = statements.ToList();
        }

        public IEnumerable<object> Statements { get; }

        public string FileName { get; }

        public override string ToString()
        {
            var @out = new StringWriter();
            foreach (var statement in this.Statements)
            {
                @out.WriteLine(statement);
            }
            return @out.ToString();
        }
    }

    public interface Statement { }

    public class HostStatement : Statement
    {
        public Uri Host { get; }
        public const string Identifier = "host";

        public HostStatement(string host)
        {
            Host = new Uri(host);
        }

        public override string ToString() => $"#! {Identifier} {Host}";
    }

    public class LimitStatement : Statement
    {
        public int Limit { get; }

        public LimitStatement(int limit)
        {
            Limit = limit;
        }
        public const string Identifier = "content-length-limit";
        public override string ToString() => $"#! {Identifier} {Limit}";
    }

    public class ExtractStatement : Statement
    {
        public string Name { get; }

        public string Expression { get; }

        public ExtractStatement(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }

        public async Task<object> ExtractFromAsync(HttpResponseMessage message)
        {

            var content = await message.Content.ReadAsStringAsync();
            var o = JToken.Parse(content);
            try
            {
                var val = o.SelectToken(Expression);
                return val;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                Console.Error.WriteLine("Can't load {0} from {1}", Expression, o);
                return null;
            }
        }

        public const string Identifier = "get";
        public override string ToString() => $"#!{Identifier} {Name} {Expression}";
    }

    public class RequestStatement : Statement
    {
        public string Request { get; }
        public IList<string> Headers { get; }
        public string Body { get; }

        public RequestStatement(string request, List<string> headers, string body)
        {
            this.Request = request;
            this.Headers = headers;
            this.Body = body;
        }

        public void Deconstruct(out string request, out IList<string> headers, out string body)
        {
            request = Request;
            headers = Headers;
            body = Body;
        }

        public override string ToString()
        {
            var r = Request;
            var h = Headers.Any() ? $"\r\n{string.Join("\r\n", Headers)}" : "";
            var b = string.IsNullOrWhiteSpace(Body) ? "" : $"\r\n{Body}";
            return r + h + b;
        }
    }

    public class CommentStatement : Statement
    {
        public List<string> Lines { get; }

        public CommentStatement(List<string> lines)
        {
            this.Lines = lines;
        }

        public override string ToString() => string.Join("\r\n", Lines);
    }
}