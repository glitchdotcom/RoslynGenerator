using FogCreek.Wasabi.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FogCreek.Wasabi.CodeGenerators
{
    public class RoslynException : InvalidOperationException
    {
        public RoslynException(string message) : base(message) { }
    }

    public partial class RoslynGenerator : IVisitor, ICodeGenVisitor
    {
        CompilationUnitSyntax currentCodeFile;
        TypeDeclarationSyntax currentType;
        NamespaceDeclarationSyntax currentNS;
        List<StatementSyntax> currentBlock;
        List<SyntaxTrivia> currentTrivia = new List<SyntaxTrivia>();

        CFunction currentAccessor;
        private const string RETURN_VARIABLE = "returnValue";
        private const string globalTypeName = "Global";

        public RoslynGenerator()
        {
            Acceptor = this;
        }

        ExpressionSyntax expression;
        ExpressionSyntax Visit(CNode node)
        {
            node.Accept(Acceptor);
            if (node is CExpression && expression == null)
                throw new RoslynException("did not generate expression");
            return expression;
        }

        bool inResumeNext = false;
        bool errObjectDeclared = false;
        int _befores = 0;
        int Before()
        {
            if (inResumeNext)
            {
                return ++_befores;
            }
            return -1;
        }

        void After(int i, StatementSyntax stmt)
        {
            if (i == -1)
            {
                currentBlock.Add(stmt);
                return;
            }
            if (_befores != i)
                throw new RoslynException("mismatched before/after");
            currentBlock.Add(SF.TryStatement()
                .WithBlock(SF.Block(stmt))
                .WithCatches(SF.SingletonList(
                    SF.CatchClause(
                        SF.CatchDeclaration(SafeIdentifierName("System.Exception"), SafeIdentifier("e")),
                        null,
                        SF.Block(SF.ParseStatement("Err.LoadFromException(e);"))))));
        }

        StatementSyntax OnErrorFlowControl(StatementSyntax flow, bool startedInResumeNext)
        {
            if (!startedInResumeNext)
                return flow;
            var err = SafeIdentifierName("Err");
            var loadFromExceptionArgs = SF.ArgumentList(SF.SeparatedList(new[] { SF.Argument(SafeIdentifierName("e")) }));
            StatementSyntax loadFromException = SF.ExpressionStatement(SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, err, SafeIdentifierName("LoadFromException")), loadFromExceptionArgs));
            var accessResume = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, err, SafeIdentifierName("Resume"));
            var catchStatements = new StatementSyntax[] {
                SF.ExpressionStatement(Assign(accessResume, Literal(inResumeNext))),
                inResumeNext ? loadFromException : SF.ThrowStatement()
            };
            CatchClauseSyntax _catch = SF.CatchClause(SF.CatchDeclaration(SafeIdentifierName("System.Exception"), SafeIdentifier("e")), null, SF.Block(catchStatements));
            return SF.TryStatement(flow as BlockSyntax ?? SF.Block(flow), SF.SingletonList(_catch), null);
        }

        #region IVisitor Members

        private void VisitBlock(CStatementBlock statements)
        {
            foreach (var node in statements)
            {
                Visit(node);
            }
        }

        void IVisitor.VisitBlock(CStatementBlock statements)
        {
            VisitBlock(statements);
        }

        void IVisitor.VisitAssignment(CAssignment assign)
        {
            int next = Before();
            ExpressionSyntax left = Visit(assign.Target);
            if (left is InvocationExpressionSyntax)
                throw new NotImplementedException("Invoke in assignment");

            After(next, AddComments(SF.ExpressionStatement(Assign(left, Visit(assign.Source)))));
        }

        private T AddComments<T>(T stmt) where T : SyntaxNode
        {
            return AddComments<T>(stmt, GetComments());
        }

        private static T AddComments<T>(T stmt, SyntaxTriviaList trivia) where T : SyntaxNode
        {
            return stmt.WithLeadingTrivia(trivia.AddRange(stmt.GetLeadingTrivia()));
        }


        private BlockSyntax TrailingComments(BlockSyntax block)
        {
            return block.WithCloseBraceToken(block.CloseBraceToken.WithLeadingTrivia(GetComments().AddRange(block.CloseBraceToken.LeadingTrivia)));
        }

        private SyntaxTriviaList GetComments()
        {
            var triviaList = SF.TriviaList(currentTrivia.ToArray());
            currentTrivia.Clear();

            return triviaList;
        }

        void IVisitor.VisitCase(CCase ccase)
        {
            throw new NotImplementedException("use RoslynGenerator.VisitCases instead of Visit(CStatementBlock)");
        }

        private IEnumerable<SwitchSectionSyntax> VisitCases(CStatementBlock cases)
        {
            var labels = new List<SwitchLabelSyntax>();

            foreach (var stmt in cases)
            {
                if (stmt is CNewline) continue;
                if (stmt is CComment) { Visit(stmt); continue; }

                var @case = (CCase)stmt;

                if (@case.IsElseCase) { labels.Add(SF.DefaultSwitchLabel()); }
                else { labels.Add(SF.CaseSwitchLabel(Visit(@case.Value))); }

                if (@case.Statements != null)
                {
                    var comments = GetComments();
                    currentBlock = new List<StatementSyntax>();
                    Visit(@case.Statements);
                    currentBlock.Add(SF.BreakStatement());
                    yield return AddComments(SF.SwitchSection(SF.List(labels), SF.List(currentBlock)), comments);
                    labels.Clear();
                    currentBlock = null;
                }
            }
            if (labels.Count > 0)
                yield return AddComments(SF.SwitchSection(SF.List(labels), SF.SingletonList<StatementSyntax>(SF.BreakStatement())));
        }

        void IVisitor.VisitInterface(CInterface iface)
        {
            VisitClass(iface, SyntaxKind.InterfaceDeclaration);
        }

        void IVisitor.VisitEnum(CEnum eration)
        {
            var oldNS = currentNS;
            var typeName = eration.RawShortName;
            currentNS = LoadNamespace(eration.RawNameSpace);

            var leadingTrivia = GetComments();

            var members = new List<EnumMemberDeclarationSyntax>();
            foreach (CClassConst classMember in eration.DirectClassMemberIterator)
            {
                members.Add(SF.EnumMemberDeclaration(classMember.RawName)
                    .WithEqualsValue(SF.EqualsValueClause(Visit(classMember.Constant.Value))));
            }

            var enumDecl = SF.EnumDeclaration(
                attributeLists: GenerateAttributes(eration),
                modifiers: SF.TokenList(GetTypeVisibility(eration)),
                identifier: SafeIdentifier(typeName),
                baseList: null,
                members: SF.SeparatedList(members, Enumerable.Repeat(SF.Token(SyntaxKind.CommaToken), members.Count)));

            currentCodeFile = currentCodeFile.ReplaceNode(currentNS, currentNS.AddMembers(AddComments(enumDecl, leadingTrivia)));
            currentNS = oldNS;
        }

        void IVisitor.VisitClass(CClass cclas)
        {
            VisitClass(cclas, SyntaxKind.ClassDeclaration);
        }

        private NamespaceDeclarationSyntax LoadNamespace(string nsName = null)
        {
            if (string.IsNullOrEmpty(nsName)) nsName = Compiler.Current.DefaultNamespace.RawValue;

            var ns = currentCodeFile.Members.OfType<NamespaceDeclarationSyntax>().SingleOrDefault(nds => nds.Name.ToString() == nsName);
            if (ns != null) return ns;

            currentCodeFile = currentCodeFile.AddMembers(SF.NamespaceDeclaration(SF.ParseName(nsName)));
            return LoadNamespace(nsName);
        }

        private void VisitClass(CClass cclas, SyntaxKind kind)
        {
            var oldType = currentType;
            var typeName = cclas.RawShortName;
            var oldNS = currentNS;
            currentNS = LoadNamespace(cclas.RawNameSpace);

            var comments = GetComments();

            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                    currentType = SF.ClassDeclaration(typeName);
                    break;
                case SyntaxKind.InterfaceDeclaration:
                    currentType = SF.InterfaceDeclaration(typeName);
                    break;
                default: throw new NotImplementedException("VisitClass " + kind);
            }

            var baseList = SF.BaseList();

            if (!cclas.IsEnum && !cclas.IsInterface)
            {
                var @base = GetType(cclas.BaseClass);
                var pts = @base as PredefinedTypeSyntax;
                if (pts == null || pts.Keyword.Kind() != SyntaxKind.ObjectKeyword)
                {
                    baseList = baseList.AddTypes(SF.SimpleBaseType(@base));
                }
            }
            foreach (CTypeRef iface in cclas.Interfaces)
                baseList = baseList.AddTypes(SF.SimpleBaseType(GetType(iface)));

            if (baseList.Types.Count > 0)
            {
                currentType = currentType.WithBaseList(baseList);
            }

            var modifiers = SF.TokenList(GetTypeVisibility(cclas));
            if (cclas.IsAbstract)
                modifiers = modifiers.Add(SF.Token(SyntaxKind.AbstractKeyword));
            if (cclas.IsSealed)
                modifiers = modifiers.Add(SF.Token(SyntaxKind.SealedKeyword));

            currentType = currentType.WithModifiers(modifiers);

            if (cclas.Constructor == null && !cclas.IsInterface)
            {
                var cons = SF.ConstructorDeclaration(typeName).WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)));
                cons = InitializeFields(cclas, cons);
                if (cons.Body.Statements.Count > 0)
                {
                    currentType = currentType.AddMember(cons);
                }
            }

            InitializeStaticFields(cclas);

            foreach (CMember member in cclas.DirectMemberIterator)
                InternalGenerateMember(cclas, member);
            foreach (CMember explicitMember in cclas.ExplicitInterfaceIterator)
                InternalGenerateMember(cclas, explicitMember);
            foreach (CMember classMember in cclas.DirectClassMemberIterator)
                InternalGenerateMember(cclas, classMember);

            currentType = currentType.WithAttributeLists(GenerateAttributes(cclas));
            currentType = AddComments(currentType, comments);

            currentCodeFile = currentCodeFile.ReplaceNode(currentNS, currentNS.AddMembers(currentType));

            currentType = oldType;
            currentNS = oldNS;
        }

        private CClass optionalattr;
        private SyntaxList<AttributeListSyntax> GenerateAttributes(IAttributed iAttributed, IEnumerable<AttributeSyntax> prelude = null)
        {
            if (optionalattr == null)
                optionalattr = CProgram.Global.FindClass("WasabiOptionalAttribute");

            var attrs = new List<AttributeSyntax>();
            if (prelude != null)
                attrs.AddRange(prelude);
            attrs.AddRange(GenerateAttributesInternal(iAttributed));
            if (iAttributed is CProgram)
            {
                return SF.List(attrs.Select(NicerAttr).Select(attr => SF.AttributeList(SF.AttributeTargetSpecifier(SF.Token(SyntaxKind.AssemblyKeyword)), SF.SingletonSeparatedList(attr))));
            }
            return SF.List(attrs.Select(NicerAttr).Select(attr => SF.AttributeList(SF.SingletonSeparatedList(attr))));
        }

        private static AttributeSyntax NicerAttr(AttributeSyntax attr)
        {
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count == 0)
                attr = attr.WithArgumentList(null);

            var name = attr.Name.ToString();
            if (name.EndsWith("Attribute")) attr = attr.WithName(SF.IdentifierName(name.Remove(name.Length - "Attribute".Length)));

            return attr;
        }

        private IEnumerable<AttributeSyntax> GenerateAttributesInternal(IAttributed iAttributed)
        {
            foreach (CAttribute myAttr in iAttributed.Attributes)
            {
                if (myAttr.Type.ActualType == optionalattr)
                {
                    // Wasabi generates [System.Runtime.InteropServices.OptionalAttribute] for Wasabi optionals 
                    // automatically elsewhere, so don't generate redundant [WasabiOptional] on them.
                }
                else if (myAttr.Type.TypeName.RawValue == "System.Attribute")
                {
                    // ignore typechecker's customattributes; they are handled elsewhere
                }
                else if (myAttr.Type.TypeName.RawValue == "System.ParamArrayAttribute")
                {
                    // C# will generate these at compile time
                }
                else
                {
                    var attrParams = SF.AttributeArgumentList(SF.SeparatedList(
                        myAttr.Parameters.Unnamed.Select(n => SF.AttributeArgument(Visit(n))).Union(
                        myAttr.Parameters.Named.Select(named_node =>
                            SF.AttributeArgument(Visit(named_node.Value))
                                .WithNameEquals(SF.NameEquals(myAttr.Type.ActualType.LookupMember(named_node.Key).RawName))
                        ))));

                    var ts = GetType(myAttr.Type);
                    yield return SF.Attribute(ts as NameSyntax, attrParams);
                }
            }
        }

        private void InternalGenerateMember(CClass cclas, CMember member)
        {
            HackyGetMemberComments(cclas, member);

            switch (member.MemberType)
            {
                case "field":
                    CVariable var = (CVariable)member.Declared[0];
                    var comments = GetComments();
                    string field_name = var.Name.RawValue;
                    var field_vis = GetVisibility(member);
                    if (var.Attributes.contains("converttoproperty"))
                    {
                        var prop_vis = field_vis;
                        string prop_name = field_name;

                        // Rename and hide original field
                        field_name = "m_" + field_name;
                        field_vis = member.IsStatic ?
                            SF.TokenList(SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.PrivateKeyword)) :
                            SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword));

                        // Generate a property where the field should have been
                        var prop = SF.PropertyDeclaration(
                            GetType(var.Type),
                            prop_name)
                        .WithModifiers(prop_vis);

                        CParameters gl_gs_params = var.Attributes.getList("converttoproperty")[0].Parameters;
                        string gls = "gl";
                        if (gl_gs_params.Unnamed.Count == 1)
                            gls = gl_gs_params.Unnamed[0].Token.Value;
                        gls = gls.ToLower();

                        var accessorList = SF.AccessorList();
                        // "return this.field;"
                        if (gls.Contains("g"))
                        {
                            accessorList = accessorList.AddAccessors(
                                SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                    SF.Block(
                                        SF.ReturnStatement(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            createThisOrStaticExpression(member.IsStatic, var.ContainingClass), SafeIdentifierName(field_name))))));
                        }
                        // "this.field = value;"
                        if (gls.Contains("l") || gls.Contains("s"))
                        {
                            accessorList = accessorList.AddAccessors(
                                SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                    SF.Block(SF.ExpressionStatement(
                                        Assign(
                                            SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                createThisOrStaticExpression(member.IsStatic, var.ContainingClass), SafeIdentifierName(field_name)),
                                            SafeIdentifierName("value"))))));

                        }
                        prop = prop.WithAccessorList(accessorList);

                        currentType = currentType.AddMember(AddComments(prop, comments));
                        comments = SF.TriviaList();
                    }
                    FieldDeclarationSyntax field = PossiblyInitializedField(var, field_name)
                        .WithModifiers(field_vis)
                        .WithAttributeLists(GenerateAttributes(var));

                    currentType = currentType.AddMember(AddComments(field, comments));
                    break;
                case "property":
                    InternalGenerateProperty(cclas, (CProperty)member);
                    break;
                case "method":
                    CFunction funx = ((CMethod)member).Function;
                    Visit(funx);
                    break;
                case "const":
                    Visit(((CClassConst)member).Constant);
                    break;
                case "override":
                    CMemberOverload cmo = (CMemberOverload)member;
                    foreach (CMember memb in cmo.Overloads)
                        InternalGenerateMember(cclas, memb);
                    break;
                default:
                    throw new NotImplementedException("InternalGenerateMember of a " + member.MemberType);
            }
        }

        private void HackyGetMemberComments(CClass cclas, CMember member)
        {
            var stmts = cclas.Statements;
            var ixStmt = stmts.IndexOf(member);
            var comments = new List<CComment>();
            for (var ixComment = ixStmt - 1; ixComment >= 0; ixComment--)
            {
                var comment = stmts[ixComment] as CComment;
                if (comment == null) break;
                comments.Insert(0, comment);
            }

            foreach (CComment comment in comments)
            {
                Visit(comment);
            }
        }

        private FieldDeclarationSyntax PossiblyInitializedField(CVariable var, string name)
        {
            if ((var.Initializer != null || var.IsArray) && IsSimpleEnough(var))
            {
                return FieldDeclaration(GetType(var.Type), name, VisitVariableInitializer(var));
            }
            else
            {
                return SimpleFieldDeclaration(GetType(var.Type), SafeIdentifier(name));
            }
        }

        private static bool IsSimpleEnough(CVariable var)
        {
            if (var.IsArray && var.Initializer == null) return true;
            return IsSimpleEnough(var.Initializer);
        }

        private static bool IsSimpleEnough(CExpression exp)
        {
            while (true)
            {
                if (exp is CThisAccess) return false;
                if (exp is CConstantExpression) return true;

                var cmu = exp as CMathUnary;
                var cast = exp as CCast;
                var access = exp as CAccess;
                var cbo = exp as CBinaryOperator;
                var concat = exp as CConcat;

                if (cmu != null) { exp = cmu.Operand; }
                else if (cast != null) { exp = cast.InnerExpression; }
                else if (access != null)
                {
                    if (access.ReferenceTarget is CConst) return true;
                    var cnew = access as CNew;
                    if (cnew != null)
                    {
                        if (cnew.Type.ActualType.CecilType == null && cnew.Type.ActualType.Constructor != null) return false;
                        return cnew.Parameters.Unnamed.Cast<CExpression>().All(IsSimpleEnough) && cnew.Parameters.Named.Select(kvp => kvp.Value).Cast<CExpression>().All(IsSimpleEnough);
                    }

                    var token = access.Token;

                    var cda = access as CDefaultAccess;
                    if (cda != null)
                    {
                        var target = cda.TargetAccess;
                        token = target.ReferenceToken;

                        if (target.IsRootAccess)
                        {
                            // Inlined globals are allowed
                            var inlinedGlobals = new[] { "array", "dictionary", "ismissing" };
                            if (inlinedGlobals.Contains(target.ReferenceToken.Value) || target.ReferenceToken.Value == "utcnow" || target.ReferenceToken.Value == "join")
                            {
                                return cda.Parameters.Unnamed.Cast<CExpression>().All(IsSimpleEnough) && cda.Parameters.Named.Select(kvp => kvp.Value).Cast<CExpression>().All(IsSimpleEnough);
                            }
                        }
                    }
                    var cma = access as CMemberAccess;
                    if (cma != null)
                    {
                        var lhsAccess = cma.MemberSource as CAccess;
                        var klass = lhsAccess.ReferenceTarget as CClass;
                        if (klass == null) return false;
                        var rhs = cma.ReferenceTarget as CProperty;
                        return rhs == null;
                    }

                    var notNullableInvalid = new[] { "vbinvaliddate", "vbinvalidint", "vbinvaliddouble" };
                    if (access.GetType() == typeof(CAccess) && notNullableInvalid.Contains(access.Token.Value))
                    {
                        return true;
                    }

                    return false;
                }
                else if (cbo != null) { return IsSimpleEnough(cbo.Left) && IsSimpleEnough(cbo.Right); }
                else if (concat != null) { return IsSimpleEnough(concat.Left) && IsSimpleEnough(concat.Right); }
                else { return false; }
            }
        }

        private void InternalGenerateProperty(CClass cclas, CProperty cprop)
        {
            var comments = GetComments();
            BasePropertyDeclarationSyntax property;
            if (cprop.GetAccessor.Arguments.Count > 0)
            {
                if (cprop != cclas.DefaultMember)
                    throw new RoslynException("Type checker should have raised an error");

                var indexer = SF.IndexerDeclaration(GetType(cprop.Type));
                if (cprop.GetAccessor.Arguments.Count > 0)
                {
                    indexer = indexer.WithParameterList(SF.BracketedParameterList(AddArguments(cprop.GetAccessor.Arguments)));
                }
                property = indexer;
            }
            else
            {
                property = SF.PropertyDeclaration(GetType(cprop.Type), cprop.GetAccessor.RawName);
            }

            if (cprop.HasExplicitInterface)
            {
                property = property.WithExplicitInterfaceSpecifier(
                        SF.ExplicitInterfaceSpecifier(SafeIdentifierName(cprop.ExplicitInterfaceName)));
                if (cprop.IsStatic)
                    property = property.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.StaticKeyword)));
                else
                    property = property.WithModifiers(SF.TokenList());
            }
            else
            {
                property = property.WithModifiers(GetVisibility(cprop));
            }

            var oldBlock = currentBlock;
            currentAccessor = cprop.GetAccessor;
            var oldDeferredMembers = deferredMembersFunction;
            deferredMembersFunction = new List<MemberDeclarationSyntax>();

            var generateBodies = !cprop.GetAccessor.Abstract && !cprop.GetAccessor.InInterface;
            var accessorList = SF.AccessorList(SF.SingletonList(GenerateAccessor(cprop.GetAccessor, SyntaxKind.GetAccessorDeclaration, generateBodies)));

            if (cprop.GetAccessor.InInterface)
                property = property.WithModifiers(SF.TokenList());
            else if (cprop.GetAccessor.Abstract)
                property = property.WithModifiers(property.Modifiers.Add(SF.Token(SyntaxKind.AbstractKeyword)));
            else if (cprop.GetAccessor.Override)
                property = property.WithModifiers(property.Modifiers.Add(SF.Token(SyntaxKind.OverrideKeyword)));
            else if (cprop.GetAccessor.Virtual)
                property = property.WithModifiers(property.Modifiers.Add(SF.Token(SyntaxKind.VirtualKeyword)));

            for (int i = 1; i < 3; i++)
            {
                CFunction func = (CFunction)cprop.Declared[i];
                if (func != null)
                {
                    func.Arguments[func.Arguments.Count - 1].IndexerValueArgument = true;
                    currentAccessor = func;

                    accessorList = accessorList.AddAccessors(GenerateAccessor(func, SyntaxKind.SetAccessorDeclaration, generateBodies));
                }
            }
            property = property.WithAccessorList(accessorList);

            currentType = currentType.WithMembers(currentType.Members.Add(AddComments(property, comments)).AddRange(deferredMembersFunction));

            currentAccessor = null;
            currentBlock = oldBlock;
            deferredMembersFunction = oldDeferredMembers;
        }

        private AccessorDeclarationSyntax GenerateAccessor(CFunction cFunction, SyntaxKind kind, bool generateBodies)
        {
            var comments = GetComments();
            AccessorDeclarationSyntax acc;
            if (generateBodies)
            {
                currentBlock = new List<StatementSyntax>();
                AddMethodBody(cFunction);
                acc = SF.AccessorDeclaration(kind, SF.Block(currentBlock));
            }
            else
            {
                acc = SF.AccessorDeclaration(kind).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken));
            }
            return AddComments(acc, comments);
        }

        private static SyntaxTokenList GetVisibility(IHasVisibility member)
        {
            SyntaxTokenList attribs;
            switch (member.Visibility)
            {
                case TokenTypes.visPrivate:
                    attribs = SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword));
                    break;
                case TokenTypes.visProtected:
                    attribs = SF.TokenList(SF.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case TokenTypes.visPublic:
                    attribs = SF.TokenList(SF.Token(SyntaxKind.PublicKeyword));
                    break;
                case TokenTypes.visInternal:
                    attribs = SF.TokenList(SF.Token(SyntaxKind.InternalKeyword));
                    break;
                default:
                    throw new RoslynException("Invalid visibility: " + member.Visibility.ToString());
            }

            if (member.IsStatic)
                attribs = attribs.Add(SF.Token(SyntaxKind.StaticKeyword));

            return attribs;
        }

        private static SyntaxToken GetTypeVisibility(IHasVisibility member)
        {
            switch (member.Visibility)
            {
                case TokenTypes.visPublic:
                    return SF.Token(SyntaxKind.PublicKeyword);
                case TokenTypes.visPrivate:
                    return SF.Token(SyntaxKind.InternalKeyword);
                case TokenTypes.visInternal:
                case TokenTypes.visProtected:
                default:
                    throw new RoslynException("Invalid type visibility: " + member.Visibility.ToString());
            }
        }

        private static IEnumerable<SyntaxToken> GetAccessLevel(CFunction func)
        {
            if (func.Abstract)
                yield return SF.Token(SyntaxKind.AbstractKeyword);
            else if (func.Virtual)
                yield return SF.Token(SyntaxKind.VirtualKeyword);
            else if (func.Override)
                yield return SF.Token(SyntaxKind.OverrideKeyword);
            else
                yield break;
        }

        private ConstructorDeclarationSyntax InitializeFields(CClass klass, ConstructorDeclarationSyntax cons)
        {
            var stmts = FieldInitializers(klass.DirectMemberIterator, SF.ThisExpression());
            return cons.WithBody(SF.Block(stmts));
        }

        private void InitializeStaticFields(CClass klass)
        {
            var stmts = FieldInitializers(klass.DirectClassMemberIterator, GetType(klass.Type));
            if (stmts.Count > 0)
            {
                var staticConstructor = SF.ConstructorDeclaration(klass.RawShortName).WithModifiers(SF.TokenList(SF.Token(SyntaxKind.StaticKeyword)));
                currentType = currentType.AddMember(staticConstructor.WithBody(SF.Block(stmts)));
            }
        }

        private List<StatementSyntax> FieldInitializers(IEnumerable<CMember> members, ExpressionSyntax thisOrStatic)
        {
            var stmts = new List<StatementSyntax>();
            foreach (var f in members.Where(m => m.MemberType == "field").Cast<CField>())
            {
                string field_name = f.Variable.Name.RawValue;
                if (f.Variable.Attributes.contains("converttoproperty"))
                    field_name = "m_" + field_name;

                if ((f.Variable.Initializer != null || f.Variable.IsArray) && !IsSimpleEnough(f.Variable))
                {
                    stmts.Add(SF.ExpressionStatement(Assign(
                        SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            thisOrStatic, SafeIdentifierName(field_name)),
                        VisitVariableInitializer(f.Variable))));
                }
            }
            return stmts;
        }

        const string xmlCommentStart = "''";
        void IVisitor.VisitComment(CComment comment)
        {
            var text = comment.Text;
            var slashies = "//";
            if (text.StartsWith(xmlCommentStart))
            {
                text = text.Substring(xmlCommentStart.Length);
                slashies = "///";
            }

            currentTrivia.Add(SF.Comment(slashies + text));
        }

        void IVisitor.VisitConcat(CConcat concat)
        {
            ExpressionSyntax left = Visit(concat.Left), right = Visit(concat.Right);

            // the weirdness with precedence here is because, when `int ix = 5`,
            //    "a" + ix + 1  => "a51"
            // but we desire:
            //    "a" + (ix + 1) => "a6"
            // Also, note that
            //    "a" + ix - 1
            // is not valid C#.

            expression = SF.BinaryExpression(
                SyntaxKind.AddExpression,
                Parenthesize(left, concat.Left is CConcat ? Precedence.Additive : Precedence.Primary),
                Parenthesize(right, concat.Right is CConcat ? Precedence.Additive : Precedence.Primary));
        }

        // increasing order of precedence
        enum Precedence
        {
            AssignmentAndLambda = 1,
            Conditional,
            NullCoalescing,
            ConditionalOr, // ||
            ConditionalAnd, // &&
            LogicalOr, // |
            LogicalXor, // ^
            LogicalAnd, // &
            Equality,
            RelationalAndTypeTesting,
            Shift,
            Additive,
            Mutliplicative,
            Unary,
            Primary,
            NotAnOperator = 99999
        }

        static Precedence GetPrecedence(ExpressionSyntax exp)
        {
            if (exp is ParenthesizedLambdaExpressionSyntax) return Precedence.AssignmentAndLambda;
            if (exp is SimpleLambdaExpressionSyntax) return Precedence.AssignmentAndLambda;

            if (exp is ConditionalExpressionSyntax) return Precedence.Conditional;

            if (exp is CastExpressionSyntax) return Precedence.Unary;
            if (exp is PrefixUnaryExpressionSyntax) return Precedence.Unary;

            if (exp is MemberAccessExpressionSyntax) return Precedence.Primary;
            if (exp is InvocationExpressionSyntax) return Precedence.Primary;
            if (exp is ElementAccessExpressionSyntax) return Precedence.Primary;
            if (exp is PostfixUnaryExpressionSyntax) return Precedence.Primary;
            if (exp is ObjectCreationExpressionSyntax) return Precedence.Primary;
            if (exp is ArrayCreationExpressionSyntax) return Precedence.Primary;
            if (exp is DefaultExpressionSyntax) return Precedence.Primary;
            if (exp is ParenthesizedExpressionSyntax) return Precedence.Primary;

            if (exp is LiteralExpressionSyntax) return Precedence.NotAnOperator;
            if (exp is IdentifierNameSyntax) return Precedence.NotAnOperator;
            if (exp is ThisExpressionSyntax) return Precedence.NotAnOperator;
            if (exp is BaseExpressionSyntax) return Precedence.NotAnOperator;
            if (exp is PredefinedTypeSyntax) return Precedence.NotAnOperator;

            if (exp is BinaryExpressionSyntax) return GetPrecedence(((BinaryExpressionSyntax)exp).Kind());

            throw new NotImplementedException(exp.GetType().ToString());
        }

        static Precedence GetPrecedence(SyntaxKind kind)
        {
            // from https://msdn.microsoft.com/en-us/library/6a71f45d.aspx
            switch (kind)
            {
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    return Precedence.Mutliplicative;
                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                    return Precedence.Additive;
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                    return Precedence.Shift;
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.AsExpression:
                    return Precedence.RelationalAndTypeTesting;
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                    return Precedence.Equality;
                case SyntaxKind.BitwiseAndExpression:
                    return Precedence.LogicalAnd;
                case SyntaxKind.ExclusiveOrExpression:
                    return Precedence.LogicalXor;
                case SyntaxKind.BitwiseOrExpression:
                    return Precedence.LogicalOr;
                case SyntaxKind.LogicalAndExpression:
                    return Precedence.ConditionalAnd;
                case SyntaxKind.LogicalOrExpression:
                    return Precedence.ConditionalOr;
                case SyntaxKind.CoalesceExpression:
                    return Precedence.NullCoalescing;

                default: throw new NotImplementedException(kind.ToString());
            }
        }

        static BinaryExpressionSyntax BinaryExpression(SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right)
        {
            return SF.BinaryExpression(
                kind,
                Parenthesize(left, GetPrecedence(kind)),
                Parenthesize(right, GetPrecedence(kind)));
        }

        static PrefixUnaryExpressionSyntax PrefixUnaryExpression(SyntaxKind kind, ExpressionSyntax operand)
        {
            return SF.PrefixUnaryExpression(kind, Parenthesize(operand, Precedence.Unary));
        }

        static ExpressionSyntax Not(ExpressionSyntax testExpression)
        {
            var binexp = testExpression as BinaryExpressionSyntax;
            if (binexp == null)
            {
                var parexp = testExpression as ParenthesizedExpressionSyntax;
                if (parexp != null)
                {
                    binexp = parexp.Expression as BinaryExpressionSyntax;
                }
            }

            if (binexp != null)
            {
                // try to simplify it
                BinaryExpressionSyntax inverted = null;
                if (binexp.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken))
                    inverted = binexp.WithOperatorToken(SF.Token(SyntaxKind.ExclamationEqualsToken));
                else if (binexp.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
                    inverted = binexp.WithOperatorToken(SF.Token(SyntaxKind.EqualsEqualsToken));
                if (inverted != null)
                {
                    if (testExpression is ParenthesizedExpressionSyntax)
                        return SF.ParenthesizedExpression(inverted);
                    else return inverted;
                }
            }
            return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, testExpression);
        }

        static CastExpressionSyntax Cast(TypeSyntax type, ExpressionSyntax inner)
        {
            return SF.CastExpression(type, Parenthesize(inner, Precedence.Unary));
        }

        void IVisitor.VisitConst(CConst cconst)
        {
            var type = GetType(cconst.Value.Type);

            if (currentFunction != null)
            {
                var lvar = SF.VariableDeclaration(type, SF.SingletonSeparatedList(SF.VariableDeclarator(SafeIdentifier(cconst.RawName), null,
                    SF.EqualsValueClause(Visit(cconst.Value)))));
                varsFunction = varsFunction.Add(AddComments(SF.LocalDeclarationStatement(lvar).AddModifiers(SF.Token(SyntaxKind.ConstKeyword))));
            }
            else
            {
                var field = FieldDeclaration(type, cconst.RawName, Visit(cconst.Value))
                .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.ConstKeyword)))
                .WithAttributeLists(GenerateAttributes(cconst));
                field = AddComments(field);

                currentType = currentType.AddMember(field);
            }
        }

        void IVisitor.VisitDim(CDim dim)
        {
            foreach (CVariable var in dim.Variables)
            {
                Visit(var);
            }
        }

        Lazy<string> labelDo;
        void IVisitor.VisitDo(CDo cdo)
        {
            bool inResume = inResumeNext;
            var comments = GetComments();

            var testExpression = Visit(cdo.Condition);

            if (cdo.IsDoUntil)
            {
                // "Do Until X" === "Do While Not X"
                testExpression = Not(testExpression);
            }

            var block = currentBlock;
            var oldDo = labelDo;
            labelDo = ExitLabel("__doExit");

            currentBlock = new List<StatementSyntax>();
            Visit(cdo.Statements);
            StatementSyntax loop;
            if (cdo.IsPostConditionLoop)
            {
                loop = SF.DoStatement(SF.Block(currentBlock), testExpression);
            }
            else
            {
                loop = SF.WhileStatement(testExpression, SF.Block(currentBlock));
            }

            loop = AddComments(loop, comments);
            AddLabeledStatement(block, OnErrorFlowControl(loop, inResume), labelDo);
            currentBlock = block;
            labelDo = oldDo;
        }

        private static void AddLabeledStatement(List<StatementSyntax> block, StatementSyntax stmt, Lazy<string> label)
        {
            if (stmt is BlockSyntax)
            {
                block.AddRange(((BlockSyntax)stmt).Statements);
            }
            else
            {
                block.Add(stmt);
            }

            if (label.IsValueCreated)
            {
                block.Add(SF.LabeledStatement(label.Value, SF.EmptyStatement()));
            }
        }

        private Dictionary<string, uint> labelNumbers = new Dictionary<string, uint>();
        /// <summary> generate a function-unique identifier for jump targets, temporary variables, etc.</summary>
        private string Label(string labelType)
        {
            uint i;
            if (!labelNumbers.TryGetValue(labelType, out i))
            {
                i = 0;
            }

            try
            {
                if (i == 0) return labelType;
                return labelType + i;
            }
            finally
            {
                labelNumbers[labelType] = ++i;
            }
        }

        Lazy<string> innermostExit;
        private Lazy<string> ExitLabel(string labelType)
        {
            innermostExit = new Lazy<string>(() => Label(labelType));
            return innermostExit;
        }

        void IVisitor.VisitExit(CExit exit)
        {
            StatementSyntax stmt;
            switch (exit.ExitType)
            {
                case "do":
                    stmt = ExitInternal(labelDo);
                    break;
                case "for":
                    stmt = ExitInternal(labelFor);
                    break;
                case "sub":
                    stmt = SF.ReturnStatement();
                    break;
                case "function":
                case "property":
                    stmt = SF.ReturnStatement(SafeIdentifierName(RETURN_VARIABLE));
                    break;
                case "select":
                    stmt = ExitInternal(labelSelect);
                    break;
                default:
                    throw new NotImplementedException("Exit " + exit.ExitType);
            }
            currentBlock.Add(AddComments(stmt));
        }

        private StatementSyntax ExitInternal(Lazy<string> label)
        {
            if (label == innermostExit) return SF.BreakStatement();

            return SF.GotoStatement(SyntaxKind.GotoStatement, SafeIdentifierName(label.Value));
        }

        private static IdentifierNameSyntax SafeIdentifierName(string p)
        {
            if (p == null) throw new ArgumentNullException("p");

            return SF.IdentifierName(SafeIdentifier(p));
        }

        private static SyntaxToken SafeIdentifier(string name)
        {
            if (name.Contains(","))
            {
                name = string.Join(", ", name.Split(',').Select(s => s.Trim()));
            }
            if (name == "event" || name == "object" || name == "delegate" || name == "string" || name == "char")
            {
                return SF.Identifier("@" + name);
            }
            return SF.Identifier(name);
        }

        void IVisitor.VisitReturn(CReturn _return)
        {
            var stmt = AddComments(SF.ReturnStatement());
            if (_return.Expression != null)
            {
                Visit(_return.Expression);
                stmt = stmt.WithExpression(expression);
            }
            currentBlock.Add(stmt);
        }

        private static InvocationExpressionSyntax InvokeConversion(string which, ExpressionSyntax expression)
        {
            ExpressionSyntax classExpression = SafeIdentifierName("Wasabi.Runtime.Conversion");
            var access = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, classExpression, SafeIdentifierName(which));
            return SF.InvocationExpression(access).WithArgumentList(SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(expression))));
        }

        void IVisitor.VisitParenExpression(CParenExpression exp)
        {
            expression = Visit(exp.InnerExpression);
            if (exp is CCast)
            {
                if (exp.InnerExpression.Type.ActualType == BuiltIns.Nothing)
                    return;//don't typecast Nothing
                else if (exp.Type.ActualType == BuiltIns.String && exp.InnerExpression.Type != exp.Type)
                    expression = InvokeConversion("CStr", expression);
                else if (exp.Type.ActualType == BuiltIns.Int32 && exp.InnerExpression.Type != exp.Type)
                    expression = InvokeConversion("CLng", expression);
                else if (exp.Type.ActualType == BuiltIns.Double && exp.InnerExpression.Type != exp.Type)
                    expression = InvokeConversion("CDbl", expression);
                else
                {
                    var type = GetType(exp.Type);
                    var pts = type as PredefinedTypeSyntax;
                    if (pts != null && pts.Keyword.Kind() == SyntaxKind.ObjectKeyword)
                    {
                        do
                        {
                            var unwrap = expression;
                            if (unwrap is ParenthesizedExpressionSyntax)
                            {
                                unwrap = ((ParenthesizedExpressionSyntax)unwrap).Expression;
                            }
                            if (unwrap is CastExpressionSyntax)
                            {
                                expression = ((CastExpressionSyntax)unwrap).Expression;
                                if (expression is ParenthesizedExpressionSyntax)
                                {
                                    expression = ((ParenthesizedExpressionSyntax)expression).Expression;
                                }
                                continue;
                            }
                        } while (false);
                    }
                    expression = Cast(type, expression);
                }
            }
            else
            {
                expression = SF.ParenthesizedExpression(expression);
            }
        }

        Lazy<string> labelFor;
        void IVisitor.VisitFor(CFor cfor)
        {
            bool inResume = inResumeNext;
            var comments = GetComments();

            var declarators = new List<VariableDeclaratorSyntax>();
            var initializers = new List<ExpressionSyntax>();
            ExpressionSyntax testExpression;
            ExpressionSyntax incrementor;
            ExpressionSyntax end;

            if (cfor.Terminator is CConstantExpression)
            {
                // This isn't necessarily true, but in FogBugz, we never have a constant terminator without a constant step
                initializers.Add(Assign(Visit(cfor.ForVariable), Visit(cfor.Initializer)));
                end = Visit(cfor.Terminator);
            }
            else
            {
                var endId = SF.Identifier(Label("__forEnd"));

                end = SF.IdentifierName(endId);
                currentBlock.Add(SF.ExpressionStatement(Assign(Visit(cfor.ForVariable), Visit(cfor.Initializer))));
                declarators.Add(SF.VariableDeclarator(endId, null, SF.EqualsValueClause(Visit(cfor.Terminator))));
            }

            var stepUnary = cfor.Step as CUnaryOperator;
            var stepConst = cfor.Step as CConstantExpression;
            var stepAccess = cfor.Step as CAccess;
            if (cfor.Step == null)
            {
                testExpression = BinaryExpression(SyntaxKind.LessThanOrEqualExpression, Visit(cfor.ForVariable), end);
                incrementor = SF.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, Visit(cfor.ForVariable));
            }
            else if (stepUnary != null)
            {
                var operand = (CConstantExpression)stepUnary.Operand; // not necessarily true, but it works for FogBugz
                if (stepUnary.Operation.Value == "-")
                {
                    testExpression = BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, Visit(cfor.ForVariable), end);
                    if (operand.Value.RawValue == "1")
                    {
                        incrementor = SF.PostfixUnaryExpression(SyntaxKind.PostDecrementExpression, Visit(cfor.ForVariable));
                    }
                    else
                    {
                        incrementor = Assign(Visit(cfor.ForVariable), Visit(operand), SyntaxKind.SubtractAssignmentExpression);
                    }
                }
                else if (stepUnary.Operation.Value == "+")
                    throw new NotImplementedException("should pass through to stepConst");
                else throw new NotImplementedException(stepUnary.Operation.Value);
            }
            else if (stepConst != null)
            {
                // It's not a CUnaryOperator, so it has to be positive
                testExpression = BinaryExpression(SyntaxKind.LessThanOrEqualExpression, Visit(cfor.ForVariable), end);
                incrementor = Assign(Visit(cfor.ForVariable), Visit(stepConst), SyntaxKind.AddAssignmentExpression);
            }
            else if (stepAccess != null)
            {
                if (!(stepAccess.ReferenceTarget is CConst)) throw new NotImplementedException("Non-`Const` `Step` access expression");

                testExpression = BinaryExpression(SyntaxKind.LessThanOrEqualExpression, Visit(cfor.ForVariable), end);
                incrementor = Assign(Visit(cfor.ForVariable), Visit(stepAccess), SyntaxKind.AddAssignmentExpression);
            }
            else
            {
                throw new NotImplementedException("Non-constant `Step` expression");
            }

            var oldBlock = currentBlock;
            var oldFor = labelFor;
            currentBlock = new List<StatementSyntax>();
            labelFor = ExitLabel("__forExit");
            Visit(cfor.Statements);

            var declaration = declarators.Count > 0 ? SF.VariableDeclaration(GetType(cfor.ForVariable.Type), SF.SeparatedList(declarators)) : null;
            var loop = SF.ForStatement(declaration, SF.SeparatedList<ExpressionSyntax>(initializers), testExpression, SF.SingletonSeparatedList(incrementor), SF.Block(currentBlock));
            AddLabeledStatement(oldBlock, OnErrorFlowControl(SF.Block(AddComments(loop, comments)), inResume), labelFor);

            currentBlock = oldBlock;
            labelFor = oldFor;
        }

        void IVisitor.VisitForEach(CForEach _foreach)
        {
            // You must declare the variable inside your foreach (that is, there is no `foreach(x in e)`, it must be `foreach(string x in e)`.
            // We create a little variable here for your convenience:
            var current = SafeIdentifier(Label("__val"));

            bool inResume = inResumeNext;
            var comments = GetComments();

            var type = GetType(_foreach.ForVariable.Type);
            var exp = Visit(_foreach.Enumerable);

            StatementSyntax setForVar = SF.ExpressionStatement(Assign(Visit(_foreach.ForVariable), SF.IdentifierName(current)));

            var block = currentBlock;
            var oldFor = labelFor;
            labelFor = ExitLabel("__forEachExit");

            currentBlock = new List<StatementSyntax>() { setForVar };
            Visit(_foreach.Statements);

            var loop = SF.ForEachStatement(type, current, exp, SF.Block(currentBlock));
            loop = AddComments(loop, comments);
            AddLabeledStatement(block, OnErrorFlowControl(loop, inResume), labelFor);
            currentBlock = block;
            labelFor = oldFor;
        }

        static ExpressionSyntax Assign(ExpressionSyntax left, ExpressionSyntax right, SyntaxKind kind = SyntaxKind.SimpleAssignmentExpression)
        {
            return SF.AssignmentExpression(kind, left, right);
        }

        /// <summary>Parenthesize expressions that have lower precedence than the parent expression </summary>
        static ExpressionSyntax Parenthesize(ExpressionSyntax operand, Precedence precedence)
        {
            if (GetPrecedence(operand) < precedence) return SF.ParenthesizedExpression(operand);
            return operand;
        }

        SyntaxList<StatementSyntax> varsFunction;
        List<MemberDeclarationSyntax> deferredMembersFunction;

        /// <summary>Wrap the function in a  `try/finally` block with this as the only statement in the `finally`</summary>
        List<StatementSyntax> tryFunction;

        CFunction currentFunction;
        void IVisitor.VisitFunction(CFunction function)
        {
            if (function.Name == "finalize") throw new NotImplementedException("finalize");

            var oldAvailableWiths = availableWiths;
            availableWiths = new List<WithInfo>();
            var oldLabelNumbers = labelNumbers;
            labelNumbers = new Dictionary<string, uint>();
            var method = MethodFromFunction(function);
            var oldDeferredMembers = deferredMembersFunction;
            deferredMembersFunction = new List<MemberDeclarationSyntax>();

            if (method is ConstructorDeclarationSyntax)
            {
                var ctor = (ConstructorDeclarationSyntax)method;
                ctor = InitializeFields(function.Class, ctor);

                if (function.BaseConstructorParameters != null && function.BaseConstructorParameters.Unnamed.Count > 0)
                {
                    ctor = ctor.WithInitializer(SF.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                        SF.ArgumentList(VisitParameters(function.BaseConstructorParameters))));
                }
                method = ctor;
            }

            GenerateOverloads(function);

            method = method.WithParameterList(SF.ParameterList(AddArguments(function.Arguments, generateOptionals: !function.HasExplicitInterface)));

            if (function.Type.RawName != "__Void")
            {
                method = method.WithReturnType(GetType(function.Type));
            }

            var oldBlock = currentBlock;

            if (!function.InInterface && !function.Abstract)
            {
                currentBlock = new List<StatementSyntax>(method.Body.Statements);
                AddMethodBody(function);
                method = method.WithBody(TrailingComments(SF.Block(currentBlock)));
            }
            else
            {
                method = ((MethodDeclarationSyntax)method).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken));
            }
            method = method.WithAttributeLists(GenerateAttributes(function));

            currentType = currentType.WithMembers(currentType.Members.Add(method).AddRange(deferredMembersFunction));

            currentBlock = oldBlock;
            availableWiths = oldAvailableWiths;
            labelNumbers = oldLabelNumbers;
            deferredMembersFunction = oldDeferredMembers;
        }

        private void ProfileStep(CFunction function)
        {
            if (!function.Attributes.contains("profilestep"))
                return;

            var sStepName = function.QualifiedName;
            if (function.Class != null)
            {
                sStepName = function.Class.Name + "." + sStepName;
            }
            if (function.Attributes["profilestep"].Parameters.Unnamed.Count >= 1)
            {
                CNode n = function.Attributes["profilestep"].Parameters.Unnamed[0];
                string fileName = ((CConstantExpression)n).Value.Value;
                sStepName = sStepName + " (" + fileName + ")";
            }

            var step = SF.InvocationExpression(
                SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.IdentifierName("Wasabi.Runtime.Profiler.MiniProfiler"), SF.IdentifierName("Step")),
                    SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(Literal(sStepName)))));

            currentBlock = new List<StatementSyntax>
            {
                SF.UsingStatement(SF.Block(currentBlock)).WithExpression(step)
            };
        }

        private BaseMethodDeclarationSyntax MethodFromFunction(CFunction function)
        {
            if (function.Class != null && function.Name == "class_terminate")
                throw CompileException.codegenFailed(string.Format("Destructors not allowed: {0}", function.Token));
            if (function.Sealed)
                throw new NotImplementedException("sealed function");

            var comments = GetComments();
            BaseMethodDeclarationSyntax method;
            var attrs = GetVisibility(function).AddRange(GetAccessLevel(function));

            if (function.Class != null && function.Class.IsAConstructor(function))
                method = SF.ConstructorDeclaration(function.Class.RawShortName).WithBody(SF.Block());
            else
            {
                method = SF.MethodDeclaration(GetType(function.Type), function.FunctionAlias);
                if (function.InInterface)
                {
                    attrs = SF.TokenList();
                }
                else if (!function.Abstract)
                {
                    method = ((MethodDeclarationSyntax)method).WithBody(SF.Block());
                    if (function.HasExplicitInterface)
                    {
                        method = ((MethodDeclarationSyntax)method).WithExplicitInterfaceSpecifier(
                            SF.ExplicitInterfaceSpecifier(SafeIdentifierName(function.ExplicitInterfaceName)));

                        if (function.IsStatic)
                            throw new NotImplementedException("static interface implementation??");
                        else
                            attrs = SF.TokenList();
                    }
                }
            }

            method = method.WithModifiers(attrs);
            method = AddComments(method, comments);

            return method;
        }

        /// <summary>
        /// Generate convenience overloads for optional parameters so C# developers have an easy way to call 
        /// without setting all the optional parameters to null
        /// </summary>
        /// <param name="function">function node being generated</param>
        private void GenerateOverloads(CFunction function)
        {
            if (function.Visibility == TokenTypes.visInternal || function.Visibility == TokenTypes.visPrivate)
                return;

            if (function.DeclaringClass == null || function.DeclaringClass.Visibility != TokenTypes.visPublic)
                return;

            if (function.HasExplicitInterface)
                return;

            for (int i = function.Arguments.MinAllowedParameters; i < function.Arguments.Count; i++)
            {
                if (!function.Arguments[i].IsOptional || function.Arguments[i].Direction.Value == "byref")
                    continue;

                var method = MethodFromFunction(function);
                method = method.WithParameterList(SF.ParameterList(AddArguments(function.Arguments.GetRange(0, i))));

                Func<CArgument, ArgumentSyntax> makeParam = arg =>
                {
                    ExpressionSyntax exp = SafeIdentifierName(arg.Name.RawValue);
                    if (arg.Direction.Value == "byref" && (string)arg.Direction.AdditionalInfo != "Implicit")
                    {
                        return SF.Argument(null, SF.Token(SyntaxKind.RefKeyword), exp);
                    }

                    return SF.Argument(exp);
                };

                var parameters = SF.ArgumentList(SF.SeparatedList(function.Arguments.Take(i).Select(makeParam)));

                parameters = parameters.AddArguments(SF.Argument(Literal(null)));

                if (method is ConstructorDeclarationSyntax)
                {
                    var cds = (ConstructorDeclarationSyntax)method;
                    method = cds.WithInitializer(SF.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, parameters));
                }
                else
                {
                    var target = createThisOrStaticExpression(function.IsStatic, function.Class);
                    var invocation = SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    target, SafeIdentifierName(function.FunctionAlias)), parameters);
                    if (function.InInterface)
                    {
                        method = ((MethodDeclarationSyntax)method).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken));
                    }
                    else if (function.Type.RawName == "__Void")
                    {
                        method = method.WithBody(method.Body.AddStatements(SF.ExpressionStatement(invocation)));
                    }
                    else
                    {
                        method = method.WithReturnType(GetType(function.Type));
                        method = method.WithBody(method.Body.AddStatements(SF.ReturnStatement(invocation)));
                    }
                }

                currentType = currentType.AddMember(method);
            }
        }

        private void AddMethodBody(CFunction function)
        {
            var oldCurrentFunction = currentFunction;
            currentFunction = function;
            var oldVars = varsFunction;
            varsFunction = new SyntaxList<StatementSyntax>();
            bool oldInResume = inResumeNext;
            inResumeNext = false;
            bool oldErrObjectDeclared = errObjectDeclared;
            errObjectDeclared = function.UsesOldErrorHandling;

            var oldTry = tryFunction;
            tryFunction = new List<StatementSyntax>();

            try
            {
                if (function.UsesOldErrorHandling)
                    Declare(SafeIdentifierName("Wasabi.Runtime.Error"), "Err", SF.ObjectCreationExpression(SafeIdentifierName("Wasabi.Runtime.Error")).WithArgumentList(SF.ArgumentList()));

                var oldBlock = currentBlock;
                currentBlock = new List<StatementSyntax>();

                SetOptionalArguments(function);

                ICodeGenAcceptor cga = Acceptor as ICodeGenAcceptor;
                if (cga != null) cga.EnterFunction(function);
                Visit(function.Statements);
                if (cga != null) cga.ExitFunction(function);
                ProfileStep(function);

                if (tryFunction.Count > 0)
                {
                    oldBlock.Add(SF.TryStatement().WithBlock(SF.Block(currentBlock)).WithFinally(SF.FinallyClause(SF.Block(tryFunction))));
                    currentBlock = oldBlock;
                }
                else
                {
                    oldBlock.AddRange(currentBlock);
                    currentBlock = oldBlock;
                }

                if (function.UsesOldReturnSyntax && function.Type.RawName != "__Void")
                {
                    currentBlock.Add(SF.ReturnStatement(SafeIdentifierName(RETURN_VARIABLE)));
                }

                if (function.Type.RawName != "__Void" && function.UsesOldReturnSyntax)
                    Declare(function.Type, RETURN_VARIABLE);

                currentBlock.InsertRange(0, varsFunction);
            }
            finally
            {
                varsFunction = oldVars;
                inResumeNext = oldInResume;
                errObjectDeclared = oldErrObjectDeclared;
                tryFunction = oldTry;
                currentFunction = oldCurrentFunction;
            }
        }

        private void SetOptionalArguments(CFunction function)
        {
            currentBlock.AddRange(from arg in function.Arguments
                                  where arg.Optional && arg.Attributes["optional"].Parameters.Unnamed.Count > 0
                                  let id = SafeIdentifierName(arg.Name.RawValue)
                                  let val = Visit(arg.Attributes["optional"].Parameters[0])
                                  where !(val is LiteralExpressionSyntax && ((LiteralExpressionSyntax)val).Kind() == SyntaxKind.NullLiteralExpression)
                                  select SF.ExpressionStatement(
                                    SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, id, BinaryExpression(SyntaxKind.CoalesceExpression, id, val))));
        }

        private SeparatedSyntaxList<ParameterSyntax> AddArguments(CArgumentList arguments, bool generateOptionals = true)
        {
            var parameters = new List<ParameterSyntax>();
            foreach (CArgument arg in arguments)
            {
                var name = arg.Name.RawValue;
                var type = AddQuestionMarkIfNecessary(arg);
                var modifiers = SF.TokenList();
                if (arg.Attributes.contains("ParamArray"))
                    modifiers = modifiers.Add(SF.Token(SyntaxKind.ParamsKeyword));
                if (arg.Direction.Value == "byref" && (string)arg.Direction.AdditionalInfo != "Implicit")
                    modifiers = modifiers.Add(SF.Token(SyntaxKind.RefKeyword));

                var param = SF.Parameter(SafeIdentifier(name))
                    .WithType(type)
                    .WithModifiers(modifiers);

                if (generateOptionals)
                {
                    IEnumerable<AttributeSyntax> prelude = null;
                    if (arg.Optional)
                    {
                        if (modifiers.Any())
                            prelude = new[] { SF.Attribute(SafeIdentifierName("System.Runtime.InteropServices.Optional")) };
                        else
                            param = param.WithDefault(SF.EqualsValueClause(Literal(null)));
                    }

                    param = param.WithAttributeLists(GenerateAttributes(arg, prelude));
                }

                parameters.Add(param);
            }
            return SF.SeparatedList(parameters);
        }

        private ExpressionStatementSyntax ResponseWrite(ExpressionSyntax arg)
        {
            var globalResponseObj = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LoadGlobal(), SafeIdentifierName("Response"));
            var responseWrite = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, globalResponseObj, SafeIdentifierName("Write"));
            return SF.ExpressionStatement(SF.InvocationExpression(responseWrite, SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(arg)))));
        }

        void IVisitor.VisitHtml(CHtml html)
        {
            if (currentFunction == null || html.HtmlString == "")
                return;
            var sHTML = Literal(html.HtmlString);

            int next = Before();
            var stmt = AddComments(ResponseWrite(sHTML));
            After(next, stmt);
        }

        void IVisitor.VisitIf(CIf cif)
        {
            bool inResume = inResumeNext;
            var comments = GetComments();

            var condition = Visit(cif.Condition);

            var block = currentBlock;

            currentBlock = new List<StatementSyntax>();
            Visit(cif.ThenStatements);
            var thenStatements = SF.Block(currentBlock);

            var elseifs = new Stack<IfStatementSyntax>();
            foreach (CNode node in cif.ElseIfBlocks)
            {
                currentBlock = new List<StatementSyntax>();
                Visit(node);
                elseifs.Push(currentBlock.Cast<IfStatementSyntax>().Single());
            }
            currentBlock = new List<StatementSyntax>();
            Visit(cif.ElseStatements);
            ElseClauseSyntax elseStatements = null;
            if (currentBlock.Count > 0)
            {
                elseStatements = SF.ElseClause(SF.Block(currentBlock));
            }
            while (elseifs.Count > 0)
            {
                var elif = elseifs.Pop();
                elseStatements = SF.ElseClause(elif.WithElse(elseStatements));
            }

            var currentIf = SF.IfStatement(condition, thenStatements, elseStatements);
            currentIf = AddComments(currentIf, comments);

            if (!cif.IsElseIf)
                block.Add(OnErrorFlowControl(currentIf, inResume));
            else
                block.Add(currentIf);
            currentBlock = block;
        }

        void IVisitor.VisitMemberVariable(CMemberVariable membervariable)
        {
            throw new NotImplementedException("See VisitClass");
        }

        void IVisitor.VisitReDim(CReDim redim)
        {
            foreach (CVariable var in redim.Variables)
            {
                int next = Before();

                var varRef = GetVariableReference((CVariableBase)var.RootForRedim);

                StatementSyntax stmt;
                if (redim.PreserveArrayContents)
                {
                    var exp = var.DimensionInitializer[0];

                    var call = SF.InvocationExpression(SF.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("System.Array"), SafeIdentifierName("Resize")))
                    .WithArgumentList(SF.ArgumentList(SF.SeparatedList(new ArgumentSyntax[]{
                        SF.Argument(varRef).WithRefOrOutKeyword(SF.Token(SyntaxKind.RefKeyword)),
                        SF.Argument(BinaryExpression(SyntaxKind.AddExpression,
                            Literal(1),
                            Visit(exp)))
                    })));

                    stmt = SF.ExpressionStatement(call);
                }
                else
                {
                    stmt = SF.ExpressionStatement(Assign(varRef, VisitVariableInitializer(var)));
                }
                After(next, AddComments(stmt));
            }
        }

        private ExpressionSyntax VisitVariableInitializer(CVariable var)
        {
            if (var.Initializer != null)
                return Visit(var.Initializer);
            if (var.IsArray)
            {
                CVariableBase root = var.RootForRedim as CVariableBase;
                if (root == null)
                    root = var;
                CParameters emptyParams = new CParameters();

                return ArrayInitializer(GetArrayType(root.Type), emptyParams, var.DimensionInitializer);
            }
            throw new NotImplementedException();
        }

        Lazy<string> labelSelect;
        void IVisitor.VisitSelect(CSelect select)
        {
            var oldBlock = currentBlock;
            var comments = GetComments();

            var oldLabelSelect = labelSelect;
            labelSelect = ExitLabel("__exitSelect");

            var exp = Visit(select.Pivot);
            var sections = SF.List(VisitCases(select.Cases));

            if (sections.Count > 0)
            {
                var switchSyntax = SF.SwitchStatement(exp, sections);
                AddLabeledStatement(oldBlock, AddComments(switchSyntax, comments), labelSelect);
            }

            labelSelect = oldLabelSelect;
            currentBlock = oldBlock;
        }

        void IVisitor.VisitSpecialEqual(CSpecialEqual specialequal)
        {
            int next = Before();
            var stmt = ResponseWrite(Visit(specialequal.Value));
            After(next, AddComments(stmt));
        }

        void IVisitor.VisitStatement(CStatement statement)
        {
            if (statement is CNewline)
                return;
            if (statement is CExpressionStatement)
            {
                int next = Before();
                var stmt = SF.ExpressionStatement(Visit(((CExpressionStatement)statement).InnerExpression));
                After(next, AddComments(stmt));
                return;
            }
            throw new NotImplementedException();
        }

        void IVisitor.VisitToken(CToken token)
        {
            throw new NotImplementedException();
        }

        void IVisitor.VisitWhile(CWhile cwhile)
        {
            bool inResume = inResumeNext;
            var comments = GetComments();
            var testExpression = Visit(cwhile.Condition);

            var block = currentBlock;
            currentBlock = new List<StatementSyntax>();
            Visit(cwhile.Statements);
            var loop = SF.WhileStatement(testExpression, SF.Block(currentBlock));
            currentBlock = block;
            currentBlock.Add(OnErrorFlowControl(AddComments(loop, comments), inResume));
        }

        private class WithInfo
        {
            public WithInfo(string label, CTypeRef type)
            {
                Label = label;
                Type = type;
            }
            internal readonly string Label;
            internal readonly CTypeRef Type;
        }

        List<WithInfo> availableWiths;
        WithInfo currentWith;

        void IVisitor.VisitWith(CWith with)
        {
            var oldWith = currentWith;
            GetNewWith(with);

            int next = Before();
            var stmt = SF.ExpressionStatement(Assign(SafeIdentifierName(currentWith.Label), Visit(with.Value)));
            After(next, AddComments(stmt));

            Visit(with.Statements);

            availableWiths.Add(currentWith);

            currentWith = oldWith;
        }

        private void GetNewWith(CWith with)
        {
            foreach (WithInfo winfo in availableWiths)
            {
                if (with.Value.Type == winfo.Type)
                {
                    currentWith = winfo;
                    availableWiths.Remove(winfo);
                    return;
                }
            }

            currentWith = new WithInfo(Label("__with"), with.Value.Type);
            Declare(GetType(with.Value.Type), currentWith.Label, null);
        }

        private void Declare(CTypeRef type, string name)
        {
            if (type.ActualType.IsObject || type.ActualType == BuiltIns.String || type.ActualType is CArrayType)
                Declare(GetType(type), name, Literal(null));
            else
                DeclareValueType(GetType(type), name);
        }

        private void DeclareValueType(TypeSyntax type, string name)
        {
            ExpressionSyntax val = SF.ObjectCreationExpression(type).WithArgumentList(SF.ArgumentList());

            var pts = type as PredefinedTypeSyntax;
            if (pts != null)
            {
                switch (pts.Keyword.Kind())
                {
                    case SyntaxKind.ByteKeyword:
                    case SyntaxKind.IntKeyword:
                        val = Literal(default(int));
                        break;
                    case SyntaxKind.LongKeyword:
                        val = Literal(default(long));
                        break;
                    case SyntaxKind.DoubleKeyword:
                        val = Literal(default(double));
                        break;
                    case SyntaxKind.BoolKeyword:
                        val = Literal(default(bool));
                        break;
                    case SyntaxKind.CharKeyword:
                        val = Literal(default(char));
                        break;
                }
            }

            Declare(type, name, val);
        }

        private void Declare(TypeSyntax type, string name, ExpressionSyntax val)
        {
            var declarator = SF.VariableDeclarator(SafeIdentifier(name));
            if (val != null)
                declarator = declarator.WithInitializer(SF.EqualsValueClause(val));

            varsFunction = varsFunction.Add(SF.LocalDeclarationStatement(SF.VariableDeclaration(type, SF.SingletonSeparatedList(declarator))));
        }

        private void DeclareInline(CTypeRef ctr, string name)
        {
            currentBlock.Add(SF.LocalDeclarationStatement(SF.VariableDeclaration(GetType(ctr), SF.SingletonSeparatedList(
                SF.VariableDeclarator(SafeIdentifier(name), null, SF.EqualsValueClause(Literal(null)))))));
        }

        void IVisitor.VisitAccess(CAccess access)
        {
            if (access.ReferenceTarget is CArgument)
            {
                expression = GetArgumentReference((CArgument)access.ReferenceTarget, access.LhsAssignmentTarget, access.ReferencedArgument != null && access.ReferencedArgument.IsOptional);
            }
            else if (access.ReferenceTarget is CVariable)
            {
                expression = GetVariableReference((CVariable)access.ReferenceTarget);
            }
            else if (access.ReferenceTarget is CFunction)
            {
                CFunction function = (CFunction)access.ReferenceTarget;
                if ((currentAccessor == function || currentFunction == function) && !access.IsCallExplicit)
                {
                    expression = SafeIdentifierName(RETURN_VARIABLE);
                }
                else if (function.CecilMethod != null)
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName(function.CecilMethod.DeclaringType.FullName), SafeIdentifierName(function.RawName));
                else
                {
                    string name = GetFunctionName(access.ReferenceToken, function);
                    ExpressionSyntax which;
                    if (function.DeclaringClass != null || currentAccessor == function)
                        which = SF.ThisExpression();
                    else
                        which = LoadGlobal();

                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, which, SafeIdentifierName(name));
                }
            }
            else if (access.ReferenceTarget is CConst)
            {
                var cconst = (CConst)access.ReferenceTarget;
                var id = SF.IdentifierName(cconst.RawName);
                if (cconst.Attributes.contains("ExecuteAtCompiler"))
                {
                    expression = Visit(cconst.Value);
                }
                else if (cconst.ContainingFunction != null)
                {
                    expression = id;
                }
                else if (cconst.ContainingClass != null)
                {
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, GetType(cconst.ContainingClass.Type), id);
                }
                else
                {
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LoadGlobalType(), id);
                }
            }
            else if (access.ReferenceTarget is CClass)
            {
                expression = GetType(access.ReferenceTarget.Type);
            }
            else
                throw new NotImplementedException();
        }

        private ExpressionSyntax GetArgumentReference(CArgument arg, bool lhsAssignmentTarget, bool referencedArgumentIsOptional)
        {
            if (arg.IndexerValueArgument)
                return SafeIdentifierName("value");
            else if (arg.Optional && !lhsAssignmentTarget)
            {
                if (referencedArgumentIsOptional)
                    return SafeIdentifierName(arg.Name.RawValue);
                else if (arg.Type.ActualType.IsObject || arg.Type.ActualType == BuiltIns.String || arg.Type.ActualType is CArrayType)
                    return SafeIdentifierName(arg.Name.RawValue);
                else
                    return Cast(GetType(arg.Type), SafeIdentifierName(arg.Name.RawValue));
            }
            else
                return SafeIdentifierName(arg.Name.RawValue);
        }

        private ExpressionSyntax GetVariableReference(CVariableBase var)
        {
            if (var is CArgument)
                return GetArgumentReference((CArgument)var, false, false);
            else if (var.Name.RawValue == "Err")
                return SafeIdentifierName("Err");
            else if (var.IsShared)
                return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, createThisOrStaticExpression(var.ContainingFunction.IsStatic, var.ContainingClass), SafeIdentifierName(GetSharedName(var)));
            else if ((var is CVariable) && ((CVariable)var).CecilField != null)
                return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName(((CVariable)var).CecilField.DeclaringType.FullName), SafeIdentifierName(((CVariable)var).CecilField.Name));
            else if (var.ContainingClass == null && var.ContainingFunction == null)
            {
                var cur = LoadGlobal();
                if (var.Attributes.contains("Lang"))
                {
                    if (cur is ThisExpressionSyntax)
                    {
                        cur = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, cur, SF.IdentifierName("Lang"));
                    }
                    else
                    {
                        var clocalizer = SF.IdentifierName("FogCreek.FogBugz.Globalization.CLocalizer");
                        cur = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, clocalizer, SF.IdentifierName("Current"));
                    }
                }
                return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, cur, SafeIdentifierName(var.Name.RawValue));
            }
            else
                return SafeIdentifierName(var.Name.RawValue);
        }

        private static string GetFunctionName(CToken referenceToken, CFunction function)
        {
            string name = function.RawName;
            if (referenceToken.Value != function.Name)
                name = function.FunctionAlias;
            return name;
        }

        private static ClassDeclarationSyntax DeclareGlobalType()
        {
            return SF.ClassDeclaration(SafeIdentifier(globalTypeName))
               //.WithBaseList(SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(SF.SimpleBaseType(SafeIdentifierName("Wasabi.Runtime.GlobalBase")))))
               .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.InternalKeyword), SF.Token(SyntaxKind.PartialKeyword)));
        }

        private static bool IsGlobalType(TypeDeclarationSyntax type)
        {
            var cds = type as ClassDeclarationSyntax;
            if (cds == null) return false;

            return cds.Identifier.ValueText == SafeIdentifier(globalTypeName).ValueText;
        }

        private ExpressionSyntax LoadGlobal()
        {
            if (IsGlobalType(currentType))
                return SF.ThisExpression();
            else
                return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LoadGlobalType(), SafeIdentifierName("Current"));
        }

        private static bool IsDefaultNS(NamespaceDeclarationSyntax nds)
        {
            var nsName = Compiler.Current.DefaultNamespace.RawValue;
            return nds.Name.ToString() == nsName;
        }

        private NameSyntax LoadGlobalType()
        {
            if (IsDefaultNS(currentNS))
                return SafeIdentifierName(globalTypeName);
            else
                return SF.QualifiedName(SafeIdentifierName(Compiler.Current.DefaultNamespace.RawValue), SafeIdentifierName(globalTypeName));
        }

        void IVisitor.VisitComparison(CComparison compare)
        {
            SyntaxKind op;
            switch (compare.Operation.Value)
            {
                case "<":
                    op = SyntaxKind.LessThanExpression;
                    break;
                case ">":
                    op = SyntaxKind.GreaterThanExpression;
                    break;

                case "<=":
                    op = SyntaxKind.LessThanOrEqualExpression;
                    break;
                case ">=":
                    op = SyntaxKind.GreaterThanOrEqualExpression;
                    break;

                case "=":
                    op = SyntaxKind.EqualsExpression;
                    break;
                case "<>":
                    op = SyntaxKind.NotEqualsExpression;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var lhs = Visit(compare.Left);
            var rhs = Visit(compare.Right);
            expression = BinaryExpression(op, lhs, rhs);
        }

        void IVisitor.VisitConstantExpression(CConstantExpression constant)
        {
            CToken valuetok = constant.Value;

            // if it is of type string then we actually need to put quote characters around it
            if (valuetok.TokenType == TokenTypes.str)
                expression = Literal(valuetok.Value);
            else if (valuetok.TokenType == TokenTypes.character)
                expression = Literal(valuetok.Value[0]);
            else if (valuetok.TokenType == TokenTypes.keyword)
            {
                // nothing in vb means null in .NET
                if (String.CompareOrdinal(valuetok.Value, "nothing") == 0)
                    expression = Literal(null);
                else if (String.CompareOrdinal(valuetok.Value, "false") == 0)
                    expression = Literal(false);
                else if (String.CompareOrdinal(valuetok.Value, "true") == 0)
                    expression = Literal(true);
                else if (String.CompareOrdinal(valuetok.Value, "dbnull") == 0)
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("System.DBNull"), SafeIdentifierName("Value"));
                else
                    throw new NotImplementedException("I don't know how to generate this: " + valuetok.Value);
            }
            else if (valuetok.TokenType == TokenTypes.number)
            {
                if (valuetok.Value.StartsWith("0x"))
                    expression = Literal(int.Parse(valuetok.Value.Substring(2), System.Globalization.NumberStyles.HexNumber), "0x{0:X}");
                else
                {
                    int val;
                    if (int.TryParse(valuetok.Value, out val))
                        expression = Literal(val);
                    else if (valuetok.Value == Int32.MinValue.ToString().Substring(1))
                        throw new NotImplementedException(valuetok.Value);
                    else
                        expression = Literal(double.Parse(valuetok.Value));
                }
            }
            else if (valuetok.TokenType == TokenTypes.pound)
            {
                DateTime dtval = DateTime.Parse(valuetok.RawValue);

                expression = SF.ObjectCreationExpression(SafeIdentifierName("System.DateTime"))
                    .WithArgumentList(SF.ArgumentList(SF.SeparatedList(new ArgumentSyntax[]{
                        SF.Argument(Literal(dtval.Ticks, "F0")),
                        SF.Argument(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("System.DateTimeKind"), SafeIdentifierName(dtval.Kind.ToString())))})));
            }
            else
            {
                throw new NotImplementedException("I don't know how to generate this: " + valuetok.Value);
            }
        }

        void IVisitor.VisitDefaultAccess(CDefaultAccess access)
        {
            if (VisitInlinedGlobals(access))
                return;

            CAccess t = access.TargetAccess;
            ExpressionSyntax texp = Visit(t);
            if ((access.ReferenceTarget is CDefaultAccess || access.ReferenceTarget is IVariable) &&
                !(access.ReferenceTarget is CFunction) && (access.ReferenceTarget.Type.ActualType is CArrayType))
            {
                expression = SF.ElementAccessExpression(texp, SF.BracketedArgumentList(VisitParameters(access.Parameters)));
            }
            else if (t is CMemberAccess && ((CMemberAccess)t).ReferencedMember.IsUnionMember)
            {
                throw new NotImplementedException("Union member invocation");
            }
            else if (t is CMemberAccess && ((CMemberAccess)t).ReferencedMember.MemberType != "field" && ((CMemberAccess)t).ReferencedMember.MemberType != "const" && ((CFunction)((CMemberAccess)t).ReferencedMember.Declared[0]).Attributes.contains("default"))
                expression = SF.ElementAccessExpression(texp, SF.BracketedArgumentList(VisitParameters(access.Parameters)));
            else if (t is CMemberAccess && ((CMemberAccess)t).ReferencedMember.MemberType == "property" && access.Parameters.Unnamed.Count == 0)
                expression = texp;
            else
            {
                var indexer = access.IsRealDefaultMethodCall && !(t.Type.ActualType is CFunctionType);
                if (indexer)
                {
                    expression = SF.ElementAccessExpression(texp, SF.BracketedArgumentList(VisitParameters(access.Parameters)));
                }
                else
                {
                    var args = VisitParameters(access.Parameters);

                    var fx = access.ReferenceTarget as CFunction;
                    var meth = access.ReferenceTarget as CMethod;
                    if (meth != null) fx = meth.Function;
                    if (fx is CLambdaFunction)
                    {
                        // can't omit optional parameters on lambdas
                    }
                    else if (t.Type.ActualType is CFunctionType)
                    {
                        // can't omit optional parameters on first-class functions
                    }
                    else if (fx != null && fx.Arguments.Count > 0 && !fx.Arguments.Last().Attributes.contains("paramarray"))
                    {
                        // omit null optional parameters from the right side
                        for (var i = fx.Arguments.Count - 1; i >= 0; i--)
                        {
                            if (IsOptionalNull(fx.Arguments[i], args[i]))
                            {
                                args = args.RemoveAt(i);
                            }
                            else
                            {
                                break;
                            }
                        }

                        // Dane's Obnoxious Arguments heuristic:
                        if (args.Count > 1 && args.Count(IsObnoxious) > 1)
                        {
                            var l = args.ToList();
                            var ixFirstObnoxious = l.FindIndex(IsObnoxious);
                            if (ixFirstObnoxious >= 0)
                            {
                                for (var ix = ixFirstObnoxious; ix < Math.Min(fx.Arguments.Count, l.Count); ix++)
                                {
                                    l[ix] = l[ix].WithNameColon(SF.NameColon(SafeIdentifierName(fx.Arguments[ix].Name.RawValue)));
                                }
                            }

                            if (fx.Visibility == TokenTypes.visPublic && fx.DeclaringClass != null && fx.DeclaringClass.Visibility == TokenTypes.visPublic)
                            {
                                // we generated overloads for this function, and removing null literals will cause ambiguity.
                            }
                            else
                            {
                                // remove named optional parameters with null literals
                                l = l.Where((arg, ix) => !IsOptionalNull(fx.Arguments[ix], arg)).ToList();
                            }

                            args = SF.SeparatedList(l);
                        }
                    }

                    expression = SF.InvocationExpression(texp, SF.ArgumentList(args));
                }
            }
        }

        private bool IsOptionalNull(CArgument formalParameter, ArgumentSyntax argument)
        {
            var les = argument.Expression as LiteralExpressionSyntax;

            return formalParameter.Optional && les != null && les.Kind() == SyntaxKind.NullLiteralExpression;
        }

        private static bool IsObnoxious(ArgumentSyntax arg)
        {
            var expr = arg.Expression;

            if (expr is LiteralExpressionSyntax) return true;

            var unary = expr as PrefixUnaryExpressionSyntax;
            if (unary != null) return unary.Operand is LiteralExpressionSyntax;

            return false;
        }

        private bool VisitInlinedGlobals(CDefaultAccess access)
        {
            CAccess t = access.TargetAccess;
            expression = null;
            if (t.IsRootAccess)
            {
                switch (t.ReferenceToken.Value)
                {
                    case "getref":
                        CFunctionType ftype = (CFunctionType)access.Type.ActualType;
                        var obj = ftype.Target.Class == null ? LoadGlobal() : SF.ThisExpression();
                        var tgt = SafeIdentifierName(ftype.Target.RawName);
                        expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, obj, tgt);
                        break;
                    case "array":
                        expression = ArrayInitializer(GetArrayType(access.Type), access.Parameters, new CParameters());
                        break;
                    case "dictionary":
                        expression = SF.ObjectCreationExpression(GetType(access.Type))
                            .WithArgumentList(SF.ArgumentList());
                        break;
                    case "ismissing":
                        expression = BinaryExpression(SyntaxKind.EqualsExpression, Literal(null),
                            SafeIdentifierName(((CArgument)((CAccess)access.Parameters[0]).ReferenceTarget).Name.RawValue));
                        break;
                }
            }
            else if (t is CMemberAccess)
            {
                var m = (CMemberAccess)t;
                switch (m.ReferenceToken.Value)
                {
                    case "raise":
                        if (!currentFunction.UsesOldErrorHandling && m.MemberSource.IsRootAccess && m.MemberSource.ReferenceToken.Value == "err" && !errObjectDeclared)
                        {
                            expression = SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("Wasabi.Runtime.Error"), SafeIdentifierName("RaiseException")))
                                .WithArgumentList(SF.ArgumentList(VisitParameters(access.Parameters)));
                        }
                        break;
                    case "createobject":
                        if (m.MemberSource.IsRootAccess && m.MemberSource.ReferenceToken.Value == "server")
                        {
                            var parameters = new List<ArgumentSyntax>();
                            var alloc = SF.ObjectCreationExpression(GetType(access.Type));
                            if (access.Type.ActualType.Constructor != null)
                                foreach (CArgument arg in ((CMethod)access.Type.ActualType.Constructor).Function.Arguments)
                                    parameters.Add(SF.Argument(Literal(null)));
                            expression = alloc.WithArgumentList(SF.ArgumentList(SF.SeparatedList(parameters)));
                        }
                        break;
                }
            }
            return expression != null;
        }

        private ExpressionSyntax AddOne(CNode node)
        {
            // This is for fixing up array initializers, e.g. `new x[1 + -1]` -> `new x[0]`
            var constant = node as CConstantExpression;
            if (constant != null)
            {
                var valuetok = constant.Value;

                if (valuetok.TokenType == TokenTypes.number)
                {
                    if (valuetok.Value.StartsWith("0x"))
                        return Literal(1 + int.Parse(valuetok.Value.Substring(2), System.Globalization.NumberStyles.HexNumber), "0x{0:X}");
                    else
                        return Literal(1 + int.Parse(valuetok.Value));
                }
                throw new NotImplementedException(valuetok.TokenType.ToString());
            }

            var unary = node as CMathUnary;
            if (unary != null)
            {
                if (unary.Operation.Value == "+") return AddOne(unary.Operand);
                constant = unary.Operand as CConstantExpression;
                if (constant != null)
                {
                    if (constant.Value.Value == "1")
                    {
                        return Literal(0);
                    }
                }
                throw new NotImplementedException("AddOne unary-anything-but-constant-1");
            }

            var math = node as CMath;
            if (math != null)
            {
                if (math.Right is CConstantExpression)
                {
                    if (math.Operation.Value == "+")
                    {
                        return BinaryExpression(SyntaxKind.AddExpression, Visit(math.Left), AddOne(math.Right));
                    }
                    else if (math.Operation.Value == "-")
                    {
                        constant = math.Right as CConstantExpression;
                        if (constant != null && constant.Value.TokenType == TokenTypes.number)
                        {
                            if (constant.Value.Value == "1")
                            {
                                return Visit(math.Left);
                            }
                            else
                            {
                                return BinaryExpression(SyntaxKind.SubtractExpression, Visit(math.Left), Literal(int.Parse(constant.Value.Value) - 1));
                            }
                        }
                    }
                }
            }

            return BinaryExpression(SyntaxKind.AddExpression, Literal(1), Visit(node));
        }

        private ExpressionSyntax ArrayInitializer(ArrayTypeSyntax ctr, CParameters parameters, CParameters diminitializers)
        {
            if (parameters.Unnamed.Count == 0)
            {
                if (diminitializers.Unnamed.Count > 1)
                {
                    var myList = new List<ArgumentSyntax>();
                    myList.Add(SF.Argument(SF.TypeOfExpression(ctr.ElementType)));
                    for (int i = 0; i < diminitializers.Unnamed.Count; i++)
                        myList.Add(SF.Argument(AddOne(diminitializers[i])));
                    var args = SF.ArgumentList(SF.SeparatedList(myList));

                    return Cast(ctr, SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SafeIdentifierName("System.Array"), SafeIdentifierName("CreateInstance"))).WithArgumentList(args));
                }
                else
                {
                    ExpressionSyntax init = Literal(0);
                    if (diminitializers.Unnamed.Count > 0 && diminitializers[0] != null)
                        init = AddOne(diminitializers[0]);
                    if (ctr.ElementType is ArrayTypeSyntax && ((ArrayTypeSyntax)ctr.ElementType).RankSpecifiers.Count > 0)
                    {
                        return Cast(ctr,
                            SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SafeIdentifierName("System.Array"), SafeIdentifierName("CreateInstance")))
                            .WithArgumentList(SF.ArgumentList(SF.SeparatedList(new[]{
                                SF.Argument(SF.TypeOfExpression(ctr.ElementType)),
                                SF.Argument(init)}))));
                    }
                    else
                    {
                        return SF.ArrayCreationExpression(ctr.WithRankSpecifiers(SF.SingletonList(SF.ArrayRankSpecifier(SF.SingletonSeparatedList(init)))));
                    }
                }
            }
            else
            {
                var expressions = SF.SeparatedList(VisitParameters(parameters).Select(arg => arg.Expression));
                var array = SF.ArrayCreationExpression(ctr, SF.InitializerExpression(SyntaxKind.ArrayInitializerExpression, expressions));
                return array;
            }
        }

        void IVisitor.VisitLogic(CLogic logic)
        {
            bool boolCompare = logic.Type.ActualType == BuiltIns.Boolean;
            SyntaxKind op = SyntaxKind.None;
            switch (logic.Operation.Value)
            {
                case "and":
                    if (boolCompare)
                        op = SyntaxKind.LogicalAndExpression;
                    else
                        op = SyntaxKind.BitwiseAndExpression;
                    break;
                case "or":
                    if (boolCompare)
                        op = SyntaxKind.LogicalOrExpression;
                    else
                        op = SyntaxKind.BitwiseOrExpression;
                    break;

                case "xor":
                    op = SyntaxKind.ExclusiveOrExpression;
                    break;
                case "eqv":
                default:
                    throw new NotImplementedException(logic.Operation.Value);
            }

            var type = GetType(logic.Type);
            var left = Visit(logic.Left);
            var right = Visit(logic.Right);
            if (!boolCompare)
            {
                if (logic.Left.Type.ActualType != logic.Type.ActualType)
                    left = Cast(type, left);
                if (logic.Right.Type.ActualType != logic.Type.ActualType)
                    right = Cast(type, right);
            }
            expression = BinaryExpression(op, left, right);
        }

        void IVisitor.VisitMath(CMath math)
        {
            SyntaxKind op;
            ExpressionSyntax left = Visit(math.Left), right = Visit(math.Right);
            switch (math.Operation.Value)
            {
                case "+":
                    op = SyntaxKind.AddExpression;
                    break;
                case "-":
                    op = SyntaxKind.SubtractExpression;
                    break;
                case "*":
                    op = SyntaxKind.MultiplyExpression;
                    break;
                case "/":
                    if (math.Left.Type.ActualType == BuiltIns.Int32 && math.Right.Type.ActualType == BuiltIns.Int32)
                    {
                        if (math.Left is CConstantExpression)
                        {
                            left = Doubleize((CConstantExpression)math.Left);
                        }
                        else if (math.Right is CConstantExpression)
                        {
                            right = Doubleize((CConstantExpression)math.Right);
                        }
                        else
                        {
                            left = Cast(SF.PredefinedType(SF.Token(SyntaxKind.DoubleKeyword)), left);
                        }
                        expression = BinaryExpression(SyntaxKind.DivideExpression, left, right);
                        return;
                    }
                    else goto case "\\";
                case "\\":
                    op = SyntaxKind.DivideExpression;
                    break;
                case "mod":
                    op = SyntaxKind.ModuloExpression;
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (math.Left.Type.ActualType.IsEnum && math.Type.ActualType == BuiltIns.Double)
            {
                //e.g. `dblBoost = dblBoost + (oTopic.nSpamState - 2.0) * 0.3D;`, oTopic.nSpamState is of type `enum SpamState`
                left = Cast(GetType(BuiltIns.Double.Type), left);
            }
            else if (math.Type == BuiltIns.Int32 && (math.Left.Type == BuiltIns.Int64 || math.Right.Type == BuiltIns.Int64))
            {
                expression = Cast(GetType(BuiltIns.Int32.Type), BinaryExpression(op, left, right));
                return;
            }
            else if (!ImplicitlyTyped(math))
            {
                var type = GetType(math.Type);
                left = Cast(type, left);
                right = Cast(type, right);
            }
            expression = BinaryExpression(op, left, right);
        }

        static ExpressionSyntax Doubleize(CConstantExpression cConstantExpression)
        {
            var valuetok = cConstantExpression.Value;

            if (valuetok.TokenType != TokenTypes.number) throw new NotImplementedException(valuetok.TokenType.ToString());
            if (valuetok.Value.StartsWith("0x")) throw new NotImplementedException("hex doubleized");

            return Literal(double.Parse(valuetok.Value));
        }

        static bool ImplicitlyTyped(CMath math)
        {
            if (math.Left.Type == math.Right.Type && math.Right.Type == math.Type) return true;
            if (math.Type == BuiltIns.Double && (math.Left.Type == BuiltIns.Double || math.Right.Type == BuiltIns.Double)) return true;
            if (math.Type == BuiltIns.Int64 && math.Left.Type != BuiltIns.Double && math.Right.Type != BuiltIns.Double) return true;

            return false;
        }

        void IVisitor.VisitMathUnary(CMathUnary math)
        {
            switch (math.Operation.Value)
            {
                case "-":
                    if (math.Operand is CConstantExpression && ((CConstantExpression)math.Operand).Value.RawValue == Int32.MinValue.ToString().Substring(1))
                        expression = Literal(Int32.MinValue);
                    else
                        expression = PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, Visit(math.Operand));
                    break;
                case "+":
                    expression = Visit(math.Operand);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        void IVisitor.VisitMemberAccess(CMemberAccess access)
        {
            if (access.ReferencedMember.IsUnionMember)
            {
                throw new NotImplementedException("Union member access");
            }

            Visit(access.MemberSource);
            var source = Parenthesize(expression, Precedence.Primary);
            switch (access.ReferencedMember.MemberType)
            {
                case "field":
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        source, SafeIdentifierName(((CVariable)access.ReferencedMember.Declared[0]).Name.RawValue));
                    break;
                case "method":
                    {
                        CFunction function = ((CFunction)access.ReferencedMember.Declared[0]);
                        string name = function.RawName;
                        if (access.ReferenceToken.Value != function.Name)
                            name = function.FunctionAlias;
                        expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, source, SafeIdentifierName(name));
                    }
                    break;
                case "property":
                    CProperty myProperty = ((CProperty)access.ReferencedMember);
                    if (!myProperty.GetAccessor.Attributes.contains("default"))
                    {
                        CFunction funcc = ((CFunction)access.ReferencedMember.Declared[0]);
                        if (funcc.Arguments.Count > 0)
                        {
                            if (!access.LhsAssignmentTarget)
                                expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, source, SafeIdentifierName("get_" + funcc.RawName));
                            else
                                throw new NotImplementedException("Named .NET properties with parameters don't yet support setting.  Sorry!");
                        }
                        else
                            expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, source, SafeIdentifierName(funcc.RawName));
                        break;
                    }
                    else break; // `expression` was already set by Visit()
                case "const":
                    CClassConst ccc = ((CClassConst)access.ReferencedMember);
                    expression = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, source, SafeIdentifierName(ccc.Constant.Name.RawValue));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        void IVisitor.VisitNew(CNew _new)
        {
            expression = SF.ObjectCreationExpression(GetType(_new.Type))
                .WithArgumentList(SF.ArgumentList(VisitParameters(_new.Parameters)));
        }

        void IVisitor.VisitNot(CNot not)
        {
            expression = Not(Visit(not.Operand));
        }

        void IVisitor.VisitOnError(COnError onerror)
        {
            var errResume = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("Err"), SafeIdentifierName("Resume"));
            var errClear = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SafeIdentifierName("Err"), SafeIdentifierName("Clear"));

            if (onerror.Action.Value == "resume")
            {
                inResumeNext = true;
                currentBlock.Add(AddComments(SF.ExpressionStatement(Assign(errResume, Literal(true)))));
            }
            else if (onerror.Action.Value == "goto")
            {
                inResumeNext = false;

                currentBlock.AddRange(new[] {
                    AddComments(SF.ExpressionStatement(Assign(errResume, Literal(false)))),
                    SF.ExpressionStatement(SF.InvocationExpression(errClear))});
            }
        }

        void IVisitor.VisitParameters(CParameters parameters)
        {
            throw new InvalidOperationException("Use this.VisitParameters(CParameters) instead");
        }

        private SeparatedSyntaxList<ArgumentSyntax> VisitParameters(CParameters parameters)
        {
            var vals = new List<ArgumentSyntax>();
            foreach (CExpression exp in parameters.Unnamed)
            {
                if (exp == null)
                    vals.Add(SF.Argument(Literal(null)));
                else
                {
                    var val = Visit(exp);

                    if (exp.IsPassedByRef)
                    {
                        var isOut = exp.ReferencedArgument != null && exp.ReferencedArgument.CecilParameter != null && !exp.ReferencedArgument.CecilParameter.IsIn && exp.ReferencedArgument.CecilParameter.IsOut;
                        vals.Add(SF.Argument(val).WithRefOrOutKeyword(SF.Token(isOut ? SyntaxKind.OutKeyword : SyntaxKind.RefKeyword)));
                    }
                    else if (exp.ReferencedArgument != null && (exp.ReferencedArgument.Type.ActualType != exp.Type.ActualType || exp.ReferencedArgument.Type.ActualType.CecilType != exp.Type.ActualType.CecilType))
                    {
                        if (exp.ReferencedArgument.Type.ActualType is CFunctionType)
                        {
                            vals.Add(SF.Argument(
                                        SF.ObjectCreationExpression(GetType(exp.ReferencedArgument.Type))
                                            .WithArgumentList(SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(val))))));
                        }
                        else
                        {
                            vals.Add(SF.Argument(val));
                        }
                    }
                    else
                    {
                        vals.Add(SF.Argument(val));
                    }
                }
            }
            return SF.SeparatedList(vals);
        }

        void IVisitor.VisitThisAccess(CThisAccess access)
        {
            expression = SF.ThisExpression();
        }

        void IVisitor.VisitBaseAccess(CBaseAccess access)
        {
            expression = SF.BaseExpression();
        }

        void IVisitor.VisitWithAccess(CWithAccess access)
        {
            expression = SafeIdentifierName(currentWith.Label);
        }

        void IVisitor.VisitTernary(CTernary ternary)
        {
            expression = SF.ConditionalExpression(
                Visit(ternary.Cond),
                Parenthesize(Visit(ternary.Left), Precedence.Conditional),
                Parenthesize(Visit(ternary.Right), Precedence.Conditional));
        }

        TryStatementSyntax currentTry;
        void IVisitor.VisitTry(CTry _try)
        {
            var lastTry = currentTry;
            var lastBlock = currentBlock;
            try
            {
                currentTry = AddComments(SF.TryStatement());

                currentBlock = new List<StatementSyntax>();
                VisitBlock(_try.Statements);
                currentTry = currentTry.WithBlock(SF.Block(currentBlock));

                VisitBlock(_try.CatchBlocks);

                if (_try.FinallyBlock != null)
                    Visit(_try.FinallyBlock);

                lastBlock.Add(currentTry);
            }
            finally
            {
                currentBlock = lastBlock;
                currentTry = lastTry;
            }
        }

        void IVisitor.VisitCatch(CCatch _catch)
        {
            var decl = AddComments(SF.CatchDeclaration(GetType(_catch.Pivot.Type), SafeIdentifier(_catch.Pivot.Name.RawValue)));

            currentBlock = new List<StatementSyntax>();
            VisitBlock(_catch.Statements);
            currentTry = currentTry.AddCatches(SF.CatchClause().WithDeclaration(decl).WithBlock(SF.Block(currentBlock)));
        }

        void IVisitor.VisitFinally(CFinally _finally)
        {
            currentBlock = new List<StatementSyntax>();
            var comments = GetComments();
            VisitBlock(_finally.Statements);
            currentTry = currentTry.WithFinally(AddComments(SF.FinallyClause(SF.Block(currentBlock)), comments));
        }

        void IVisitor.VisitThrow(CThrow _throw)
        {
            var thr = AddComments(SF.ThrowStatement());
            if (_throw.Expression != null)
                thr = thr.WithExpression(Visit(_throw.Expression));

            currentBlock.Add(thr);
        }

        private ExpressionSyntax createThisOrStaticExpression(bool isStatic, CClass type)
        {
            if (isStatic)
                return SafeIdentifierName(type.RawShortName);
            else
                return SF.ThisExpression();
        }

        void IVisitor.VisitVariable(CVariable var)
        {
            if ((var.AssignmentCount == 0 || (var.AssignmentCount == 1 && var.Initializer != null && var.Type.ActualType is CDictionaryType)) && var.AccessCount == 0)
                return;

            var type = GetType(var.Type);

            bool init = var.Initializer != null || (var.DimensionInitializer != null && (var.DimensionInitializer.Unnamed.Count > 0 && var.DimensionInitializer[0] != null));
            if (var.ContainingClass != null && var.ContainingFunction == null)
            {
                throw new NotImplementedException();
            }
            else if (var.ContainingFunction != null)
            {
                if (var.IsShared)
                {
                    string name = GetSharedName(var);
                    bool inStatic = var.ContainingFunction.IsStatic;

                    FieldDeclarationSyntax field;
                    if (init)
                        field = PossiblyInitializedField(var, name);
                    else
                        field = SimpleFieldDeclaration(type, SafeIdentifier(name));

                    field = field
                        .WithModifiers(inStatic ? SF.TokenList(SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.PrivateKeyword)) :
                        SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword)));
                    deferredMembersFunction.Add(field);

                    if (field.Declaration.Variables.Single().Initializer != null) return;

                    if (init)
                    {
                        var field_inited_identifier = SafeIdentifier(name + "_inited");
                        var field_inited = SimpleFieldDeclaration(GetType(BuiltIns.Boolean.Type), field_inited_identifier)
                            .WithModifiers(field.Modifiers);
                        deferredMembersFunction.Add(field_inited);

                        var next = Before();
                        var self = createThisOrStaticExpression(inStatic, var.ContainingClass);
                        var cond = SF.IfStatement(
                            Not(
                                SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, self, SF.IdentifierName(field_inited_identifier))),
                            SF.Block(SF.List(new[]{
                                SF.ExpressionStatement(Assign(
                                    SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, self, SafeIdentifierName(name)),
                                    VisitVariableInitializer(var))),
                                SF.ExpressionStatement(Assign(
                                    SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, self, SF.IdentifierName(field_inited_identifier)),
                                    Literal(true)))
                            })));
                        After(next, AddComments(cond));
                    }
                }
                else
                {
                    Declare(var.Type, var.Name.RawValue);

                    if (init)
                    {
                        var next = Before();
                        var comments = GetComments();
                        var stmt = SF.ExpressionStatement(Assign(
                            SafeIdentifierName(var.Name.RawValue),
                            VisitVariableInitializer(var)));
                        After(next, AddComments(stmt, comments));
                    }
                }
            }
            else
            {
                if (!IsGlobalType(currentType)) throw new RoslynException("expected global type");
                if (var.Attributes.contains("Lang")) { return; }

                var m = PossiblyInitializedField(var, var.Name.RawValue)
                    .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.InternalKeyword)));
                currentType = currentType.AddMember(AddComments(m));

                if (init && m.Declaration.Variables[0].Initializer == null)
                {
                    var next = Before();
                    var stmt = SF.ExpressionStatement(Assign(
                        SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LoadGlobal(), SafeIdentifierName(var.Name.RawValue)),
                        VisitVariableInitializer(var)));
                    After(next, stmt);
                }
            }
        }

        static string GetSharedName(CVariableBase var)
        {
            return string.Format("__{0}_{1}", var.ContainingFunction.RawName, var.Name.RawValue);
        }

        private ArrayTypeSyntax GetArrayType(CTypeRef type)
        {
            CArrayType array = (CArrayType)type.ActualType;
            if (array is CDictionaryType) throw new NotImplementedException("GetArrayType(CDictionaryType)");

            TypeSyntax elementType = SF.PredefinedType(SF.Token(SyntaxKind.ObjectKeyword));
            if (array.ItemType.ActualType != null)
                elementType = GetType(array.ItemType);

            return SF.ArrayType(elementType, SF.SingletonList(SF.ArrayRankSpecifier(SF.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SF.OmittedArraySizeExpression(), array.Dimensions)))));
        }

        private TypeSyntax AddQuestionMarkIfNecessary(CArgument arg)
        {
            var type = arg.Type;
            if (arg.Optional && !type.ActualType.IsObject && type.ActualType != BuiltIns.String && !(type.ActualType is CArrayType))
            {
                return SF.NullableType(GetType(type));
            }
            return GetType(type);
        }

        private TypeSyntax GetType(CTypeRef type)
        {
            if (type.ActualType is CDictionaryType)
            {
                var dictType = (CDictionaryType)type.ActualType;
                SyntaxToken identifier = SafeIdentifier("Wasabi.Runtime.WasabiDictionary");
                TypeArgumentListSyntax typeArguments = SF.TypeArgumentList(SF.SingletonSeparatedList(GetType(dictType.ItemType)));

                return SF.GenericName(identifier, typeArguments);
            }
            else if (type.ActualType is CArrayType)
            {
                return GetArrayType(type);
            }
            else if (type.ActualType is CFunctionType)
                return GetDelegate((CFunctionType)type.ActualType);

            string rawname = type.TypeName + " (unresolved)";
            if (type.ActualType != null)
            {
                if (type.ActualType.CecilType != null)
                {
                    var fullName = type.ActualType.CecilType.FullName;

                    switch (fullName)
                    {
                        case "System.String": return SF.PredefinedType(SF.Token(SyntaxKind.StringKeyword));
                        case "System.Char": return SF.PredefinedType(SF.Token(SyntaxKind.CharKeyword));
                        case "System.Int32": return SF.PredefinedType(SF.Token(SyntaxKind.IntKeyword));
                        case "System.Int64": return SF.PredefinedType(SF.Token(SyntaxKind.LongKeyword));
                        case "System.Boolean": return SF.PredefinedType(SF.Token(SyntaxKind.BoolKeyword));
                        case "System.Double": return SF.PredefinedType(SF.Token(SyntaxKind.DoubleKeyword));
                        case "System.Decimal": return SF.PredefinedType(SF.Token(SyntaxKind.DecimalKeyword));
                        case "System.Byte": return SF.PredefinedType(SF.Token(SyntaxKind.ByteKeyword));
                        case "System.Object": return SF.PredefinedType(SF.Token(SyntaxKind.ObjectKeyword));
                        default: return SafeIdentifierName(fullName);
                    }
                }
                if (type.ActualType is CUnionType)
                    return SF.PredefinedType(SF.Token(SyntaxKind.ObjectKeyword));
                if (type.ActualType.Token.Filename != null)
                {
                    if (type.ActualType.RawNameSpace == currentNS.Name.ToString())
                    {
                        return SafeIdentifierName(type.ActualType.RawShortName);
                    }
                    return SafeIdentifierName(type.ActualType.RawName);
                }
                rawname = type.RawName;
            }
            switch (rawname)
            {
                case "Date": return SafeIdentifierName("System.DateTime");
                case "DbNull": return SafeIdentifierName("System.DBNull");
                case "__Void": return SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword));
                default: throw new NotImplementedException(rawname);
            }
        }

        private SimpleNameSyntax GetDelegate(CFunctionType type)
        {
            if (type.CecilType != null)
            {
                return SafeIdentifierName(type.CecilType.FullName);
            }

            CFunction func = type.Target;
            string baseType;
            if (func.FunctionType == FunctionType.Function)
                baseType = "System.Func";
            else
                baseType = "System.Action";

            return GenericTypeName(func.Arguments, func.Type, baseType);
        }

        private SimpleNameSyntax GenericTypeName(CArgumentList argTypes, CTypeRef rType, string name)
        {
            var typeArgs = argTypes.Select(AddQuestionMarkIfNecessary).ToList();

            if (rType != BuiltIns.Void)
                typeArgs.Add(GetType(rType));

            if (typeArgs.Count > 0)
            {
                return SF.GenericName(SafeIdentifier(name), SF.TypeArgumentList(SF.SeparatedList(typeArgs)));
            }
            return SafeIdentifierName(name);
        }

        void IVisitor.VisitPictureOf(CPictureOfExpression pic)
        {
            ExpressionSyntax obj;
            CDefaultAccess access = pic.AccessTarget as CDefaultAccess;
            if (access.TargetAccess is CMemberAccess)
                obj = Visit(((CMemberAccess)access.TargetAccess).MemberSource);
            else
                obj = LoadGlobal();

            CNode node = access.TargetAccess.ReferenceTarget;
            CFunction func = node as CFunction;
            if (func == null)
                func = ((CMethod)node).Function;

            SimpleNameSyntax pictureOf = GenericTypeName(func.Arguments, BuiltIns.Void.Type, "PictureOf");

            var cmre = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LoadGlobal(), SafeIdentifierName("Response")),
                pictureOf);

            var name = SafeIdentifierName(GetFunctionName(access.TargetAccess.ReferenceToken, func));
            var firstArg = SF.Argument(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, obj, name));

            expression = SF.InvocationExpression(cmre, SF.ArgumentList(VisitParameters(access.Parameters).Insert(0, firstArg)));
        }

        private static LiteralExpressionSyntax Literal(int i)
        {
            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i));
        }

        private static LiteralExpressionSyntax Literal(int i, string format)
        {
            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(String.Format(format, i), i));
        }

        private static LiteralExpressionSyntax Literal(long l)
        {
            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(l));
        }

        private static LiteralExpressionSyntax Literal(double d)
        {
            var s = d.ToString();
            if (s.Contains('.') || s.Contains("E"))
            {
                s += 'D';
            }
            else
            {
                s += ".0";
            }

            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(s, d));
        }

        private static LiteralExpressionSyntax Literal(double d, string format)
        {
            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(d.ToString(format), d));
        }

        private static LiteralExpressionSyntax Literal(char ch)
        {
            return SF.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SF.Literal(ch));
        }

        private static LiteralExpressionSyntax Literal(bool b)
        {
            return SF.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }

        private static ExpressionSyntax Literal(string str)
        {
            if (str == null) return SF.LiteralExpression(SyntaxKind.NullLiteralExpression);

            var les = SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(str));
            if (!(str.Contains("\t") || str.Contains("\r") || str.Contains("\n")))
                return les;
            else
            {
                string text = "@" + les.Token.Text
                       .Replace("\\\"", "\"\"")
                       .Replace("\\t", "\t")
                       .Replace("\\r", "\r")
                       .Replace("\\n", "\n")
                       .Replace("\\\\", "\\");
                var valueText = les.Token.ValueText;
                return les.ReplaceToken(les.Token, SF.Token(SF.TriviaList(), SyntaxKind.StringLiteralToken, text, valueText, SF.TriviaList()));
            }
        }

        void IVisitor.VisitOption(COption option)
        {
        }

        private static FieldDeclarationSyntax SimpleFieldDeclaration(TypeSyntax type, SyntaxToken name)
        {
            return SF.FieldDeclaration(SF.VariableDeclaration(type, SF.SingletonSeparatedList(SF.VariableDeclarator(name))));
        }

        private static FieldDeclarationSyntax FieldDeclaration(TypeSyntax type, string name, ExpressionSyntax initializer)
        {
            return SF.FieldDeclaration(SF.VariableDeclaration(type, SF.SingletonSeparatedList(SF.VariableDeclarator(name).WithInitializer(SF.EqualsValueClause(initializer)))));
        }

        void IVisitor.VisitLambdaExpression(CLambdaExpression lambda)
        {
            var parameters = AddArguments(lambda.LambdaFunction.Arguments, generateOptionals: false);

            var oldBlock = currentBlock;
            currentBlock = new List<StatementSyntax>();

            AddMethodBody(lambda.LambdaFunction);

            expression = MakeLambdaExpression(parameters, currentBlock);

            currentBlock = oldBlock;
        }

        private static ExpressionSyntax MakeLambdaExpression(SeparatedSyntaxList<ParameterSyntax> parameters, IList<StatementSyntax> body)
        {
            CSharpSyntaxNode innerBody = SF.Block(body);
            if (body.Count == 1)
            {
                var stmt = body.Single();
                var ret = stmt as ReturnStatementSyntax;
                var ess = stmt as ExpressionStatementSyntax;
                if (ret != null)
                {
                    // `x => { return y; }` -> `x => y`
                    innerBody = ret.Expression;
                }
                if (ess != null)
                {
                    // `x => { y(); }` -> `x => y()`
                    innerBody = ess.Expression;
                }
            }

            // remove types from parameters; they can be inferred
            parameters = SF.SeparatedList(parameters.Select(param => param.WithType(null)));

            if (parameters.Count == 1)
            {
                // `(x) => body` -> `x => body`
                return SF.SimpleLambdaExpression(parameters.Single(), innerBody);
            }
            return SF.ParenthesizedLambdaExpression(SF.ParameterList(parameters), innerBody);
        }

        private SyntaxList<AttributeListSyntax> programAttributes;
        void IVisitor.VisitProgram(CProgram program)
        {
            programAttributes = GenerateAttributes(program);
        }

        private readonly List<StatementSyntax> fileFuncStatements = new List<StatementSyntax>();
        void IVisitor.VisitFile(CFile file)
        {
            currentNS = null;
            var name = Path.ChangeExtension(file.Filename.Replace('\\', '-').Replace('/', '-'), "cs");
            BeginCodeFile(name);

            currentType = DeclareGlobalType();
            currentBlock = new List<StatementSyntax>();
            varsFunction = SF.List<StatementSyntax>();
            deferredMembersFunction = new List<MemberDeclarationSyntax>();
            currentNS = LoadNamespace();

            Visit(file.Statements);

            currentBlock.InsertRange(0, varsFunction);

            bool skipBody = currentBlock.Count == 0;

            if (!skipBody)
            {
                fileFuncStatements.AddRange(currentBlock);
            }
            currentBlock = null;

            if (deferredMembersFunction.Count > 0) throw new NotImplementedException();

            currentNS = LoadNamespace();
            if (currentType.Members.Count > 0)
            {
                currentCodeFile = currentCodeFile.ReplaceNode(currentNS, currentNS.WithMembers(currentNS.Members.Insert(0, currentType)));
            }
            else if (currentNS.Members.Count == 0)
            {
                const SyntaxRemoveOptions keepTrivia = SyntaxRemoveOptions.KeepExteriorTrivia | SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.KeepTrailingTrivia;
                currentCodeFile = currentCodeFile.RemoveNode(currentNS, keepTrivia);
            }
            GetComments(); // we should save these somewhere

            EndCodeFile(name);
        }

        private static IEnumerable<StatementSyntax> RunOnce(string fileFuncHasRunName)
        {
            yield return SF.IfStatement(
                SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.ThisExpression(), SafeIdentifierName(fileFuncHasRunName)),
                SF.Block(SF.ReturnStatement()));
            yield return SF.ExpressionStatement(
                Assign(
                SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SF.ThisExpression(), SafeIdentifierName(fileFuncHasRunName)),
                Literal(true)));
        }

        private static string EscapeFileName(CFile file)
        {
            return file.Filename.Replace('.', '_').Replace('/', '_').Replace('\\', '_').Replace('-', '_');
        }

        void IVisitor.VisitDirective(CDirective directive)
        {
            // ignore directives
        }

        #endregion

        #region ICodeGenVisitor Members

        void ICodeGenVisitor.InstrumentNode(CNode node, int num)
        {
        }

        void ICodeGenVisitor.PreVisitFile(CFile file)
        {
        }

        private IVisitor Acceptor { get; set; }
        IVisitor ICodeGenVisitor.Acceptor { get { return Acceptor; } set { Acceptor = value; } }

        void ICodeGenVisitor.print(string s)
        {
            throw new NotImplementedException();
        }

        void ICodeGenVisitor.println(string s)
        {
            throw new NotImplementedException();
        }

        void ICodeGenVisitor.println()
        {
            throw new NotImplementedException();
        }

        #endregion


        private void BeginCodeFile(string name)
        {
            currentCodeFile = SF.CompilationUnit();
            currentNS = null;
            if (!Compiler.Current.DefaultNamespaceSet)
                throw new NotImplementedException("Sorry, I didn't get around to implementing no-default-namespace file generation");
        }

        private void EndCodeFile(string name)
        {
            var node = currentCodeFile;
            currentCodeFile = null;

            if (node.Members.Count == 0) return;

            string path = Compiler.Current.OutputPath;
            if (!Path.IsPathRooted(path)) path = Path.Combine(Directory.GetCurrentDirectory(), path);

            node = node.NormalizeWhitespace();

            var filename = Path.Combine(path, name);
            using (var output = new StreamWriter(filename))
            {
                node.WriteTo(output);
            }
        }

        /// <summary>Generate the implicit root file with Global.Current</summary>
        public void RootFile(string name)
        {
            BeginCodeFile(name);

            currentCodeFile = currentCodeFile.WithAttributeLists(programAttributes);
            currentNS = LoadNamespace();

            var rootGlobalType = DeclareGlobalType();

            var gtype = SF.IdentifierName(globalTypeName);

            const string globalCurrentPrivateName = "__current";

            var field = SimpleFieldDeclaration(gtype, SafeIdentifier(globalCurrentPrivateName))
                .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword), SF.Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SF.SingletonList(SF.AttributeList(SF.SingletonSeparatedList(
                    SF.Attribute(SafeIdentifierName("System.ThreadStatic"))))));
            rootGlobalType = rootGlobalType.AddMembers(field);

            var Current = SF.PropertyDeclaration(gtype, SafeIdentifier("Current"))
                .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.InternalKeyword), SF.Token(SyntaxKind.StaticKeyword)));

            var fileFunc = SafeIdentifier("fileFuncStatements");
            rootGlobalType = rootGlobalType.AddMembers(SF.MethodDeclaration(SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)), fileFunc)
                .WithBody(SF.Block(fileFuncStatements)));

            var globalCurrentInitializer = SF.Block(
                SF.ExpressionStatement(Assign(
                    SafeIdentifierName(globalCurrentPrivateName),
                    SF.ObjectCreationExpression(gtype).WithArgumentList(SF.ArgumentList()))),
                SF.ExpressionStatement(SF.InvocationExpression(
                    SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SafeIdentifierName(globalCurrentPrivateName),
                        SF.IdentifierName(fileFunc)))));

            var cond = SF.IfStatement(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        Literal(null),
                        SafeIdentifierName(globalCurrentPrivateName)),
                globalCurrentInitializer);

            Current = Current.WithAccessorList(SF.AccessorList(SF.SingletonList(SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                SF.Block(
                    cond,
                    SF.ReturnStatement(SafeIdentifierName(globalCurrentPrivateName)))))));

            rootGlobalType = rootGlobalType.AddMembers(Current);

            currentCodeFile = currentCodeFile.ReplaceNode(
                currentNS,
                currentNS.AddMembers(rootGlobalType));
            currentType = null;

            EndCodeFile(name);
        }

        private static SyntaxTriviaList SummaryTrivia(string text)
        {
            var leading = SF.TriviaList(SF.DocumentationCommentExterior("/// "));
            var empty = SF.TriviaList();
            return SF.TriviaList(SF.Trivia(
                SF.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SF.List<XmlNodeSyntax>()
                        .Add(SF.XmlText().AddTextTokens(
                    SF.XmlTextLiteral(leading, " " + text, " " + text, trailing: empty),
                    SF.XmlTextNewLine(empty, Environment.NewLine, Environment.NewLine, trailing: empty))))));
        }

        void IVisitor.VisitLock(CLock @lock)
        {
            var comments = GetComments();

            var oldBlock = currentBlock;
            currentBlock = new List<StatementSyntax>();
            var exp = Visit(@lock.Value);
            Visit(@lock.Statements);
            var lockStatement = SF.LockStatement(exp, SF.Block(currentBlock));

            currentBlock = oldBlock;
            currentBlock.Add(AddComments(lockStatement, comments));
        }

        void IVisitor.VisitOnExit(COnExit onexit)
        {
            string label = Label("__OnExit");
            // add variable to trigger the on exit handler
            Declare(SF.PredefinedType(SF.Token(SyntaxKind.BoolKeyword)), label, Literal(false));

            // set variable
            currentBlock.Add(AddComments(SF.ExpressionStatement(Assign(
                SafeIdentifierName(label), Literal(true)))));

            // add handler to current try
            var oldBlock = currentBlock;
            currentBlock = new List<StatementSyntax>();
            Visit(onexit.Statement);
            tryFunction.Add(SF.IfStatement(SafeIdentifierName(label), SF.Block(currentBlock)));

            currentBlock = oldBlock;
        }

        void IVisitor.VisitOptionalByRef(COptionalByRef optbyref)
        {
            string obrName = Label("__optByRef");

            DeclareInline(optbyref.Type, obrName);

            expression = SafeIdentifierName(obrName);

        }

        void IVisitor.VisitAttribute(CAttribute attr)
        {
            throw new NotImplementedException("visitattribute");
        }

        void IVisitor.VisitGlobalAccess(CGlobalAccess cga)
        {
        }

    }
}
