using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CFile : CNode, IAttributed, INodeParent
    {
        private String filename;
        private readonly string fullPathToFile;
        private bool includedByExecuteAtCompilerFile = false;
        private CAttributeList attribs = new CAttributeList();
        private CStatementBlock statements = new CStatementBlock();
        private List<CLambdaFunction> lambdas = new List<CLambdaFunction>();
        private List<CToken> nameSpaceResolutionPrefixes = new List<CToken>();
        private Dictionary<string, CToken> classNameAliases = new Dictionary<string, CToken>(StringComparer.InvariantCultureIgnoreCase);

        public CFile(String filename, string fullPathToFile)
        {
            this.filename = filename;
            this.fullPathToFile = fullPathToFile;
            statements.Parent = this;
        }

        public string Filename
        {
            get { return filename; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CAttributeList Attributes
        {
            get { return attribs; }
        }

        public List<CLambdaFunction> Lambdas
        {
            get { return lambdas; }
        }

        public string FullPathToFile
        {
            get { return fullPathToFile; }
        }

        public bool IncludedByExecuteAtCompilerFile
        {
            get { return includedByExecuteAtCompilerFile; }
            set { includedByExecuteAtCompilerFile = value; }
        }

        public List<CToken> NameSpaceResolutionPrefixes
        {
            get { return nameSpaceResolutionPrefixes; }
        }

        public Dictionary<string, CToken> ClassNameAliases
        {
            get { return classNameAliases; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFile(this);
        }

        public void Replace(CNode child, CNode newchild)
        {
            throw new NotImplementedException();
        }
    }
}