using System.IO;

namespace xtofs.httpscript
{
    internal class LineReader
    {
        private TextReader reader;

        public LineReader(TextReader reader)
        {
            this.reader = reader;
            MoveNext();
        }

        public string Current { get; private set; }

        public bool IsEof { get; private set; }


        public bool MoveNext()
        {
            Current = reader.ReadLine();
            IsEof = Current == null;
            return !IsEof;
        }
    }
}