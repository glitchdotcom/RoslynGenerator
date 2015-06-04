using System;

namespace FogCreek.Wasabi
{
    [Serializable]
    public class CompileException : ApplicationException
    {
        internal readonly string filename;
        internal readonly string type;
        internal readonly int errno;
        internal readonly int line;
        internal readonly string source;

        private CompileException(string filename, string type, int errno, string message, int line, string source)
            : base(escape(message))
        {
            this.filename = filename;
            this.type = type;
            this.errno = errno;
            this.line = line;
            this.source = escape(source);
        }

        internal static string escape(string str)
        {
            str = str.Replace("\n", @"\n");
            str = str.Replace("\r", @"\r");
            str = str.Replace("\t", @"\t");
            return str;
        }

        internal static CompileException codegenFailed(string p)
        {
            return new CompileException("", "C", 1, p, 0, "");
        }
    }
}