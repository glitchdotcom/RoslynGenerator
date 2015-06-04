using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public enum FunctionType
    {
        Sub,
        Function,
        PropertyGet,
        PropertySet,
    }

    public class CFunction : CStatement, IVariable, IAttributed, IHasVisibility
    {
        public const FunctionType vbPropertyGet = FunctionType.PropertyGet;
        public const FunctionType vbPropertySet = FunctionType.PropertySet;
        public const FunctionType vbFunction = FunctionType.Function;
        public const FunctionType vbSub = FunctionType.Sub;

        public Mono.Cecil.MethodDefinition CecilMethod;

        public CScope scope = new CScope();
        private CAttributeList attribs = new CAttributeList();
        private CAttributeList returnAttribs = new CAttributeList();
        private readonly TokenTypes visibility;
        private FunctionType functionType; // 1 if function, 0 if sub
        private String funcName; // the name of the function
        private String rawFuncName;
        private CStatementBlock statements = new CStatementBlock(); // a vector of CStatements that make up the function

        private CArgumentList arguments = new CArgumentList();
        // a vector of strings which are the arguments to the function

        public bool InitializedAttributes = false;

        private List<CNode> variables = new List<CNode>();
        private CClass klass;

        public CClass Class
        {
            get { return klass; }
            set { klass = value; }
        }

        private String alias;
        private int callCount = 0;
        private readonly List<CLambdaFunction> lambdas = new List<CLambdaFunction>();
        private bool accessedBeforeUsed = false;
        private int assignCount = 0;
        private int accessCount = 0;
        private List<CFunction> calledBy = new List<CFunction>();
        private Nullable<bool> calledByAPictureFunction;
        private bool usesOldErrorHandling = false;
        private bool usesNewErrorHandling = false;
        private CFunction matchSig = null;
        private bool isVirtual = false;
        private bool isAbstract = false;
        private bool isOverride = false;
        private bool isSealed = false;

        /// <summary>Creates a new instance of CFunction 
        /// this one is for funcs and subs that are parts of classes.  this means
        /// that they can be public or private.  also, we track if they are subs or funcs as this has
        /// implications with how exit/return is used 
        /// </summary>
        public CFunction(CToken token, string rawname, string name, TokenTypes visibility, FunctionType subFuncFlag,
                         CArgumentList args, CTypeRef tref)
            : base(token)
        {
            funcName = name;
            rawFuncName = rawname;
            this.visibility = visibility;
            functionType = subFuncFlag;
            LoadType(tref);

            if (args != null)
                arguments = args;

            switch (functionType)
            {
                case vbPropertyGet:
                    alias = "get_" + rawname;
                    break;

                case vbPropertySet:
                    alias = "set_" + rawname;
                    break;

                case vbSub:
                case vbFunction:
                default:
                    alias = rawname;
                    break;
            }
        }

        bool IVariable.IsArray
        {
            get { return Type.ActualType is CArrayType; }
        }

        bool IVariable.AccessedBeforeUsed
        {
            get { return accessedBeforeUsed; }
        }

        int IVariable.AssignmentCount
        {
            get { return assignCount; }
        }

        int IVariable.AccessCount
        {
            get { return accessCount; }
        }

        public virtual String Signature
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                switch (Visibility)
                {
                    case TokenTypes.visPrivate:
                        sb.Append("Private ");
                        break;
                    case TokenTypes.visProtected:
                        sb.Append("Protected ");
                        break;
                    case TokenTypes.visPublic:
                        sb.Append("Public "); // public is no longer the default; internal is the default.
                        break;
                    case TokenTypes.visInternal:
                        sb.Append("Internal ");// but we explicitly state it anyway.
                        break;
                    default:
                        throw new Exception("Invalid type for CFunction.Visibility: " + Visibility.ToString());
                }

                if (IsStatic)
                    sb.Append("Static ");

                switch (functionType)
                {
                    case vbPropertyGet:
                        sb.Append("Property Get");
                        break;

                    case vbPropertySet:
                        sb.Append("Property Set");
                        break;

                    case vbSub:
                        sb.Append("Sub");
                        break;

                    case vbFunction:
                    default:
                        sb.Append("Function");
                        break;
                }

                sb.Append("(");

                bool first = true;
                foreach (CArgument arg in arguments)
                {
                    if (!first)
                        sb.Append(", ");
                    else
                        first = false;

                    if (arg.Direction.Value == "byref")
                        sb.Append("ByRef ");
                    else
                        sb.Append("ByVal ");

                    if (arg.Type.Resolved)
                        sb.Append(arg.Type.RawName);
                }

                sb.Append(") As ");

                if (Type.Resolved)
                    sb.Append(Type.RawName);

                return sb.ToString();
            }
        }

        public virtual String TypeSignature
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                switch (functionType)
                {
                    case vbPropertySet:
                    case vbSub:
                        sb.Append("Sub");
                        break;

                    case vbPropertyGet:
                    case vbFunction:
                    default:
                        sb.Append("Function");
                        break;
                }

                sb.Append("(");

                bool first = true;
                foreach (CArgument arg in arguments)
                {
                    if (!first)
                        sb.Append(", ");
                    else
                        first = false;

                    if (arg.Direction.Value == "byref")
                        sb.Append("ByRef ");
                    else
                        sb.Append("ByVal ");

                    if (arg.Type.Resolved)
                        sb.Append(arg.Type.RawName);
                }

                if (functionType == FunctionType.PropertyGet || functionType == FunctionType.Function)
                {
                    if (!first)
                        sb.Append(", ");

                    sb.Append("Returns ");

                    if (Type.Resolved)
                        sb.Append(Type.RawName);
                }

                sb.Append(")");

                return sb.ToString();
            }
        }

        public CClass DeclaringClass
        {
            get { return klass; }
        }

        public TokenTypes Visibility
        {
            get { return visibility; }
        }

        bool isStatic = false;
        public bool IsStatic
        {
            get { return isStatic; }
            set { isStatic = value; }
        }

        public string VisibilityString
        {
            get
            {
                switch (Visibility)
                {
                    default:
                    case TokenTypes.visPrivate:
                        return "Private";
                    case TokenTypes.visProtected:
                        return "Protected";
                    case TokenTypes.visPublic:
                        return "Public";
                    case TokenTypes.visInternal:
                        return "Internal";
                }
            }
        }

        public FunctionType FunctionType
        {
            get { return functionType; }
            set { functionType = value; }
        }

        public CAttributeList Attributes
        {
            get { return attribs; }
        }

        public CAttributeList ReturnAttributes
        {
            get { return returnAttribs; }
        }

        public string Name
        {
            get { return funcName; }
            protected set { funcName = value; }
        }

        public string RawName
        {
            get { return rawFuncName; }
            protected set { rawFuncName = value; }
        }

        public string QualifiedName
        {
            get
            {
                if (Class == null)
                    return RawName;
                else
                    return Class.RawName + "." + RawName;
            }
        }

        public void Rename(string rawName)
        {
            rawFuncName = rawName;
            alias = rawName;
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CArgumentList Arguments
        {
            get { return arguments; }
            protected set { arguments = value; }
        }

        public List<CNode> Variables
        {
            get { return variables; }
        }

        public int CallCount
        {
            get { return callCount; }
            set { callCount = value; }
        }

        public List<CLambdaFunction> Lambdas
        {
            get { return lambdas; }
        }

        public int AssignCount
        {
            get { return assignCount; }
        }

        public bool Virtual
        {
            get { return isVirtual; }
            set { isVirtual = value; }
        }

        public bool Abstract
        {
            get { return isAbstract; }
            set { isAbstract = value; }
        }

        public bool InInterface
        {
            get { return Class != null && Class.IsInterface; }
        }

        public bool Override
        {
            get { return isOverride; }
            set { isOverride = value; }
        }

        public bool Sealed
        {
            get { return isSealed; }
            set { isSealed = value; }
        }

        public bool HasBody
        {
            get { return !Abstract && (Class == null || !Class.IsInterface); }
        }

        public bool UsesOldErrorHandling
        {
            get { return usesOldErrorHandling; }
            set { usesOldErrorHandling = value; }
        }

        public bool UsesNewErrorHandling
        {
            get { return usesNewErrorHandling; }
            set { usesNewErrorHandling = value; }
        }

        public bool UsesOldReturnSyntax { get; set; }

        public CFunction MustMatchSig
        {
            get { return matchSig; }
            set { matchSig = value; }
        }

        [Obsolete("This is only used by interpreted code, use FunctionAlias instead.")]
        public String Alias
        {
            get { return alias; }
        }

        public String FunctionAlias
        {
            get { return alias; }
            set { alias = value; }
        }

        private int ixCalledByCurrent = -1;
        public bool CalledByAPictureFunction
        {
            get
            {
                if (Compiler.Current.CurrentPhase < CompilerPhase.XmlGenerating)
                    throw new InvalidOperationException("Cannot access CalledByPictureFunction while type checking");

                if (!calledByAPictureFunction.HasValue)
                {
                    bool called = Attributes.contains("picture");

                    if (!called)
                    {
                        int oldixCalledByCurrent = ixCalledByCurrent;
                        for (int ix = ixCalledByCurrent + 1; ix < CalledBy.Count; ix++)
                        {
                            CFunction function = CalledBy[ix];
                            ixCalledByCurrent = ix;
                            if (this != function && function.CalledByAPictureFunction)
                            {
                                called = true;
                                break;
                            }

                        }
                        ixCalledByCurrent = oldixCalledByCurrent;
                    }

                    if (calledByAPictureFunction.HasValue)
                        calledByAPictureFunction = calledByAPictureFunction.Value || called;
                    else
                        calledByAPictureFunction = called;
                }
                return calledByAPictureFunction.Value;
            }
        }

        public List<CFunction> CalledBy
        {
            get { return calledBy; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFunction(this);
        }

        public virtual void incAssignmentCount(CClass currentclass, CFunction currentfunction)
        {
            assignCount++;
        }

        public virtual void incAccessCount(CClass currentclass, CFunction currentfunction)
        {
            if (assignCount == 0)
                accessedBeforeUsed = true;
            accessCount++;
        }

        public virtual bool canAssign(CClass currentclass, CFunction currentfunction)
        {
            return currentfunction == this;
        }

        public virtual CParameters BaseConstructorParameters
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is CMethod)
                return base.Equals(((CMethod)obj).Function);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region IVariable Members


        public void SetExternallyReferenced()
        {
            throw new InvalidOperationException();
        }

        #endregion

        bool hasExplicit = false;
        string strExplicit = "Error In Compiler";
        internal bool HasExplicitInterface { get { return hasExplicit; } }
        internal string ExplicitInterfaceName
        {
            get
            {
                if (hasExplicit)
                    return strExplicit;
                throw new InvalidOperationException(QualifiedName + " does not have an explicit interface.");
            }
        }

        internal void SetExplicitInterface(string ifaceName)
        {
            hasExplicit = true;
            strExplicit = ifaceName;
        }
    }
}