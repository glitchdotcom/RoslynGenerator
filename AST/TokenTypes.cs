using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // this defines all the constants we use throughout thistle- not just token types
    public enum TokenTypes
    {
        maskPreserveCase = 0x10000000,
        maskTokenNumber = 0x000000FF,
        maskEscaped = 0x20000000,
        maskExpConnector = 0x40000000,
        maskPreprocDir = unchecked((int)0x80000000),
        maskVisibility = 0x01000000,

        // token types
        compareOp = 0x50000001, // <, <=, >, >=, <>
        str = 0x10000002, // "blah", strings
        character = 0x1000002b, // "c"c characters
        number = 0x00000003, // for now any int or double . . .
        comment = 0x10000004, // 'blah
        oParen = 0x50000005, // (
        cParen = 0x50000006, // )
        oCurly = 0x50000024, // {
        cCurly = 0x50000025, // }
        html = 0x10000007, // some html blocks
        arithOp = 0x50000008, // +,-,*,/
        strConcat = 0x50000009, // the &
        dot = 0x5000000A, //  .
        colon = 0x1000000B, // :
        newline = 0x1000000C, // the newline char (need to make : into newline)
        space = 0x1000000D, // this is for whitespace
        equal = 0x5000000E, // =
        comma = 0x5000000F, // commas seperate parameters etc
        underscore = 0x10000010, // imp for rolling over lines
        pound = 0x00000011, // # sign. used for dates
        aspDirective = 0x00000023, // ASP directive @
        questionMark = 0x5000002b, // ternary conditional operator

        // identifier like tokens
        keyword = 0x00000014, // empty, false, nothing, dbnull, true
        vbFunc = 0x00000015, // some vb function like Cstr
        declaration = 0x00000016, // Class, Const, Dim, ReDim, Function, Sub . . .
        assignment = 0x00000017, // set
        controlFlow = 0x00000018, // if, then, else . . .
        conjunction = 0x4000001A, // and, or, not
        call = 0x0000001B, // Call
        me = 0x0000001C, // this, me
        identifier = 0x0000001D, // any word thats not a keyword (like a var or func)
        visPublic = 0x0100001E,
        visPrivate = 0x0100001F,
        visProtected = 0x01000029,
        visInternal = 0x0100002a,

        AspStart = unchecked((int)0x80000020),
        XxxEnd = unchecked((int)0x80000022),
        PreprocessorIf = unchecked((int)0x80000026),
        PreprocessorEndIf = unchecked((int)0x80000027),
        PreprocessorElse = unchecked((int)0x80000028),

        EOF = 0x000FFFF, // the end of a file
        EMPTY = 0, // used to for an invalid value;
        LastToken = 0x0000002c,
    }

    public static class TokenStrings
    {
        // specific words to look for that have special meaning.
        // within the list of the 'vbfuncs' are also functions that we have 
        public static readonly String keywords =
            "false|nothing|dbnull|true|byref|byval|erase|preserve|on|pictureof|optional|shared|paramarray";

        public static readonly String declarations =
            "abstract|class|const|dim|redim|function|sub|lambda|option|property|default|union|inherits|implements|overridable|override|interface|sealed|enum|static";

        public static readonly String assignments = "set";
        public static readonly String callStatement = "call";
        public static readonly String meObject = "me|this|base|global";

        public static readonly String controlFlows =
            "do|until|in|select|case|while|loop|for|to|else|next|exit|each|wend|if|then|with|elseif|end|resume|goto|step|try|catch|finally|throw|return";

        public static readonly String vbFuncs =
            "getarray|setarray|getfieldsize|setlocale|isempty|getlocale|rnd|sizeofmatches|getmatchat|getsubmatchat|getappvar|setappvar|formatnumber|dateserial|dateadd|datediff|cdate|isdate|formatdatetime|date|hour|minute|second|now|year|month|day|weekday|getAppVar|setAppVar|cbyte|instrrev|instr|string|now|replace|array|lbound|ubound|right|split|trim|ltrim|rtrim|clng|cint|cstr|isnumeric|strcomp|len|lenb|left|isdbnull|mid|cdbl|lcase|typename|round|asc|ascw|chr|chrw|int|cbool|hex|ucase|sqr|isarray|isobject|abs|log|exp|space|join|strreverse|getref|timevalue|randomize|timer";

        public static readonly String conjunctions = "and|or|not|imp|xor|eqv";
        public static readonly String arithOpWords = "mod";

        public static string ToString(TokenTypes type)
        {
            switch (type)
            {
                case TokenTypes.oParen: return "'('";
                case TokenTypes.cParen: return "')'";
                case TokenTypes.oCurly: return "'{'";
                case TokenTypes.cCurly: return "'}'";
            }
            return type.ToString();
        }
    }
}
