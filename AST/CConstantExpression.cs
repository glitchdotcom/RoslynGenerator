using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CConstantExpression : CExpression
    {
        private CToken m_value;

        public CConstantExpression(CToken tok)
            : base(tok)
        {
            m_value = tok;
            CClass type = null;
            switch (tok.TokenType)
            {
                case TokenTypes.number:
                    int i;
                    if (Int32.TryParse(tok.Value, out i) || tok.Value.StartsWith("0x"))
                        type = BuiltIns.Int32;
                    else
                        type = BuiltIns.Double;
                    break;

                case TokenTypes.str:
                    type = BuiltIns.String;
                    break;

                case TokenTypes.character:
                    type = BuiltIns.Character;
                    break;

                case TokenTypes.pound:
                    type = BuiltIns.Date;
                    break;

                case TokenTypes.keyword:
                    if (tok.Value == "true")
                        type = BuiltIns.Boolean;
                    else if (tok.Value == "false")
                        type = BuiltIns.Boolean;
                    else if (tok.Value == "nothing")
                        type = BuiltIns.Nothing;
                    else if (tok.Value == "dbnull")
                        type = BuiltIns.DbNull;
                    break;
            }
            if (type != null)
                base.LoadType(type);
        }

        public override bool IsConstant
        {
            get
            {
                return true;
            }
        }

        public CToken Value
        {
            get { return m_value; }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitConstantExpression(this);
        }
    }
}
