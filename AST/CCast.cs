using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CCast : CParenExpression
    {
        public CCast(CTypeRef type, CExpression exp)
            : base(type.TypeName, exp)
        {
            LoadType(type);
        }
    }
}
