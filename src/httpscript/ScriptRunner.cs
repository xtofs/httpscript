using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace xtofs.httpscript
{
    public class ScriptRunner
    {
        private readonly HttpClient client;
        private Uri baseUri;
        private int? contentLimit;
        private HttpResponseMessage lastResponse;
        private readonly Dictionary<string, object> environment = new Dictionary<string, object>();


        public ScriptRunner(Uri baseUri)
        {
            this.baseUri = baseUri;
            this.client = new HttpClient();
        }

        public async Task RunScriptAsync(Script script, string outputFilePath, bool modify = false)
        {
            using var writer = File.CreateText(outputFilePath);
            Console.WriteLine("running {0}", script.FileName);
            foreach (var statement in script.Statements)
            {
                switch (statement)
                {
                    case RequestStatement request:
                        Console.WriteLine($"running request {request.Request}");
                        lastResponse = await RunRequestAsync(request, writer, modify) ?? lastResponse;
                        this.contentLimit = null;
                        break;

                    case CommentStatement comment:
                        Console.WriteLine($"writing text '{comment.Lines.FirstOrDefault()}'");
                        writer.WriteLine("{0}", string.Join("\r\n", comment.Lines));
                        break;

                    case HostStatement host:
                        Console.WriteLine($"setting host to '{host.Host}'");
                        this.baseUri = host.Host;
                        break;

                    case LimitStatement limit:
                        Console.WriteLine($"setting limit {limit.Limit}");
                        this.contentLimit = limit.Limit;
                        break;

                    case ExtractStatement extract:
                        var val = await extract.ExtractFromAsync(lastResponse);
                        if (val == null)
                        {
                            Console.WriteLine("No value could be extracted at {0}", extract.Expression);
                            writer.WriteLine("No value could be extracted at `{0}`\r\n", extract.Expression);

                        }
                        else
                        {
                            this.environment[extract.Name] = val;
                            Console.WriteLine("Extracted {0} at {1}", val, extract.Expression);
                            writer.WriteLine("Extracted '{0}' at `{1}` into `{2}`\r\n", val, extract.Expression, extract.Name);
                        }
                        break;

                    default:
                        throw new NotImplementedException($"Statment of Type {statement.GetType()}");
                }
            }
        }

        private async Task<HttpResponseMessage> RunRequestAsync(RequestStatement statement, TextWriter writer, bool modify)
        {
            try
            {
                var request = await CreateMessageAsync(baseUri, statement);

                writer.WriteHttp(await request.ShowAsync());
                if (modify || request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
                {
                    var response = await client.SendAsync(request);

                    foreach (var header in ResponseHeadersToHide)
                    {
                        response.Headers.Remove(header);
                    }

                    // writer.WriteLine("```\r\n{0}\r\n```", await response.ShowAsync(contentLimit));
                    writer.WriteHttp(await response.ShowAsync(contentLimit));
                    return response;
                }
                else
                {
                    writer.WriteLine("```\r\n# Request not sent. Test mode enabled.\r\n```");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{0}", ex.Message);
            }
            return null;
        }

        // https://github.com/OData/WebApi/blob/4f8dc99320ac94f740fb825eb9e6095d57934c8e/src/System.Net.Http.Formatting/HttpContentMessageExtensions.cs
        private async Task<HttpRequestMessage> CreateMessageAsync(Uri uri, RequestStatement statement)
        {
            var (requestLine, headers, body) = statement;
            requestLine = FixVersion(requestLine, out var version);

            StringBuilder message = new StringBuilder();
            message.Append(requestLine);
            message.Append("\r\n");
            message.Append("Host: " + uri.Host + (uri.Port != 80 ? $":{uri.Port}" : string.Empty));
            message.Append("\r\n");
            foreach (string header in headers)
            {
                message.Append(header);
                message.Append("\r\n");
            }
            message.Append("\r\n");
            if (body != null)
            {
                message.Append(body);
            }

            var text = message.ToString();
            text = ReplaceVariables(text, environment);

            StringContent content = new StringContent(text);
            content.Headers.ContentType = HttpRequestMediaType;

            try
            {
                // TODO  
                var request = await content.ReadAsHttpRequestMessageAsync(uri.Scheme);
                request.Version = version;
                return request;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine("{0}", ex.Message);
                return null;
            }
        }

        private string ReplaceVariables(string text, Dictionary<string, object> environment)
        {
            var result = new StringBuilder();
            foreach (var part in Regex.Split(text, "({{[a-zA-Z][a-zA-Z0-9]*}})"))
            {
                if (part.StartsWith("{{"))
                {
                    var name = part.Trim('{', '}');
                    result.Append(environment.TryGetValue(name, out var val) ? val.ToString() : $"!!undefined {name}!!");
                }
                else
                {
                    result.Append(part);
                }
            }

            return result.ToString();
        }

        private static string FixVersion(string requestLine, out Version version)
        {
            var match = Regex.Match(requestLine, "HTTP/([0-9]).([0-9])$");
            if (!match.Success)
            {
                requestLine += " HTTP/1.1";
                version = new Version(1, 1);
            }
            else
            {
                version = new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }

            return requestLine;
        }

        private static IEnumerable<string> ResponseHeadersToHide = new[] { "Date", "Server", "Transfer-Encoding" };

        public static readonly MediaTypeHeaderValue HttpRequestMediaType;

        static ScriptRunner()
        {
            MediaTypeHeaderValue.TryParse("application/http; msgtype=request", out HttpRequestMediaType);
        }
    }


    internal static class WriterExtensions
    {
        public static void WriteHttp(this TextWriter writer, string text, bool codeblock = true)
        {
            if (codeblock)
            {
                writer.WriteLine("```\r\n{0}\r\n```\r\n", text);
            }
            else
            {
                writer.WriteLine("{0}", Indent(text, "> "));
            }
        }

        private static string Indent(string text, string indent)
        {
            using var reader = new StringReader(text);
            using var writer = new StringWriter();
            while (reader.TryReadLine(out var line))
            {
                writer.WriteLine("{0}{1}", indent, line);
            }
            return writer.ToString();
        }
    }
}