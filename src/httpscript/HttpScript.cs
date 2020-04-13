using System;
using System.IO;
using System.Threading.Tasks;

namespace xtofs.httpscript
{
    public static class HttpScript
    {
        public static async Task RunAsync(string inputFilePath, string outputFilePath = null, bool modify = true)
        {
            outputFilePath = outputFilePath ?? Path.ChangeExtension(inputFilePath, "md");
            var parser = new ScriptParser();
            var script = parser.Load(inputFilePath);

            // Console.WriteLine(script.ToString());

            var runner = new ScriptRunner(new Uri("https://localhost:5001"));
            await runner.RunScriptAsync(script, outputFilePath, modify);
        }
    }
}