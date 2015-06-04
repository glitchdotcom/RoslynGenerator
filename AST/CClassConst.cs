using System;

namespace FogCreek.Wasabi.AST
{
    public class CClassConst : CMember
    {
        CClass declaringClass;
        CConst myConst;
        public CClassConst(CClass declaringClass, CConst myConst)
            : base(myConst.Token, myConst.Name.Value, "const", 1, false)
        {
            this.declaringClass = declaringClass;
            this.myConst = myConst;
            Declared[0] = myConst;
            myConst.ClassConst = this;
        }

        public CConst Constant
        {
            get { return myConst; }
        }

        public override TokenTypes Visibility
        {
            get { return TokenTypes.visPublic; }
        }

        public override CClass DeclaringClass
        {
            get { return declaringClass; }
        }

        public override bool SemanticallyComplete
        {
            get { return myConst.SemanticallyComplete; }
        }
    }
}
