using System;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using xtofs.httpscript;

namespace xtofs.httpscript
{
    class Program
    {
        static async Task Main(string[] args)
        {

            await HttpScript.RunAsync("demo.http", modify: true);
        }
    }
}