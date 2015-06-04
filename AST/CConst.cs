using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{

    // this class is for statements in vbscript that define constants
    public class CConst : CStatement, INodeParent, IAttributed, IVariable
    {
        private CToken constName;
        private CExpression constValue;

        public CConst(CToken token, CToken name, CExpression exp)
            : base(token)
        {
            constName = name;
            constValue = exp;
            constValue.Parent = this;
        }

        public CToken Name
        {
            get { return constName; }
        }

        public CExpression Value
        {
            get { return constValue; }
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == constValue)
                constValue = (CExpression)newchild;
            newchild.Parent = this;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitConst(this);
        }

        private CClassConst ccc;
        public CClassConst ClassConst
        {
            get { return ccc; }
            set { ccc = value; }
        }

        #region IAttributed Members

        CAttributeList attributes = new CAttributeList();
        public CAttributeList Attributes
        {
            get { return attributes; }
        }

        #endregion

        #region IVariable Members


        string IVariable.Name
        {
            get { return constName.Value; }
        }

        public bool IsArray
        {
            get { return false; }
        }

        public bool AccessedBeforeUsed
        {
            get { return false; }
        }

        public int AssignmentCount
        {
            get { return 1; }
        }

        int accesses;
        public int AccessCount
        {
            get { return accesses; }
        }

        public string RawName
        {
            get { return constName.RawValue; }
        }

        public void incAssignmentCount(CClass currentclass, CFunction currentfunction)
        {
            throw new Exception("Const variable is readonly");
        }

        public void incAccessCount(CClass currentclass, CFunction currentfunction)
        {
            accesses++;
        }

        public bool canAssign(CClass currentclass, CFunction currentfunction)
        {
            return false;
        }

        public void SetExternallyReferenced()
        {
            throw new NotImplementedException();
        }

        #endregion

        public CClass ContainingClass { get; set; }

        public CFunction ContainingFunction { get; set; }
    }
}