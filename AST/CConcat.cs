using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{

    public class CConcat : CBinaryOperator
    {
        public CConcat(CToken tok, CExpression lhs, CExpression rhs)
            : base(tok, lhs, rhs)
        {
        }

        private delegate bool fIsConst(object o);

        public override bool IsConstant
        {
            get
            {
                CClass __string = BuiltIns.String;
                fIsConst isConst = delegate(object o)
                                       {
                                           if (o is CConstantExpression)
                                               return ((CConstantExpression)o).Type == __string;
                                           if (o is CConcat)
                                               return ((CConcat)o).IsConstant;
                                           return false;
                                       };
                return isConst(Left) && isConst(Right);
            }
        }

        private delegate string fGetConst(object o);

        public string ConstantValue
        {
            get
            {
                if (!IsConstant)
                    throw new InvalidOperationException("Not a constant node");
                fGetConst getConst = delegate(object o)
                                         {
                                             if (o is CConstantExpression)
                                                 return ((CConstantExpression)o).Value.Value;
                                             if (o is CConcat)
                                                 return ((CConcat)o).ConstantValue;
                                             throw new InvalidOperationException("Not a constant node");
                                         };
                return getConst(Left) + getConst(Right);
            }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitConcat(this);
        }
    }
}