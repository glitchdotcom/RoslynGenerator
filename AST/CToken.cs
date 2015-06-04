using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FogCreek.Wasabi.AST
{
    [DebuggerDisplay("Location: {Filename}:{LineNumber}, Value: {Value}")]
    public class CToken : Object
    {
        private String filename;
        private int line;
        private int offset;
        private string fullLine;
        private TokenTypes type;
        private String m_value;
        private String rawvalue;
        private bool spacebefore;
        private int ix = -1;
        public Object AdditionalInfo;

        public CToken(string filename, int line, int offset, string fullLine, TokenTypes typ, string val, string rawval, bool hasspacebefore)
        {
            this.filename = filename;
            this.line = line;
            this.offset = offset;
            this.fullLine = fullLine;
            type = typ;
            m_value = val;
            rawvalue = rawval;
            spacebefore = hasspacebefore;
        }

        public CToken(String filename, TokenTypes typ, String val)
            : this(filename, 0, 0, "", typ, val, val, false)
        {
        }

        public CToken()
        {
            type = (TokenTypes)(-1);
            m_value = "";
        }

        private readonly static CToken Empty = new CToken();

        public static CToken Identifer(CToken @src, String ident)
        {
            if (src == null) src = Empty;
            return new CToken(src.filename, src.line, src.offset, src.fullLine, TokenTypes.identifier, ident.ToLower(), ident, true);
        }

        public static CToken Identifer(CToken @src, String ident, String rawIdent)
        {
            if (src == null) src = Empty;
            return new CToken(src.filename, src.line, src.offset, src.fullLine, TokenTypes.identifier, ident, rawIdent, true);
        }

        public static CToken Keyword(CToken @src, String keyword)
        {
            if (src == null) src = Empty;
            return new CToken(src.filename, src.line, src.offset, src.fullLine, TokenTypes.keyword, keyword.ToLower(), keyword, true);
        }

        internal static CToken String(CToken @src, string alias)
        {
            if (src == null) src = Empty;
            return new CToken(src.filename, src.line, src.offset, src.fullLine, TokenTypes.str, alias, alias, true);
        }

        public string Filename
        {
            get { return filename; }
        }

        public int LineNumber
        {
            get { return line; }
        }

        public int ByteOffset
        {
            get { return offset; }
        }

        public string FullLine
        {
            get { return fullLine; }
        }

        public TokenTypes TokenType
        {
            get { return type; }
        }

        public string Value
        {
            get { return m_value; }
        }

        public string RawValue
        {
            get
            {
                if (rawvalue == null) return m_value;
                return rawvalue;
            }
        }

        public bool HasSpaceBefore
        {
            get { return spacebefore; }
        }

        public int Index
        {
            get { return ix; }
            set { ix = value; }
        }

        public String ToDebugString()
        {
            return string.Format("Location: {0}:{1}, Value: {2}", Filename, LineNumber, Value);
        }

        public override String ToString()
        {
            return m_value;
        }
    }
}