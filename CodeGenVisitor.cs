using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FogCreek.Wasabi.AST;

namespace FogCreek.Wasabi
{
    public class Counter
    {
        internal int count = 0;

        public int intValue()
        {
            return count;
        }

        public void increment()
        {
            count++;
        }
    }

    public class CodeGenVisitor : IVisitor, ICodeGenAcceptor
    {
        internal ICodeGenVisitor visitor;
        internal bool instrument;
        internal Counter instrumentnumber;
        internal StreamWriter instrumentdb;

        internal const int DbAll = -1;
        internal const int DbMsSql = 0x01;
        internal const int DbMySql = 0x02;
        internal const int DbAccess = ~(0x03);

        internal int dbtype = DbAll;

        internal List<IInterceptor> functionInterceptors = new List<IInterceptor>();

        public CodeGenVisitor(StreamWriter instrumentdb, ICodeGenVisitor visitor, bool instrument, Counter counter)
        {
            this.instrumentdb = instrumentdb;
            this.visitor = visitor;
            this.instrument = instrument;
            this.visitor.Acceptor = this;
            instrumentnumber = counter;
        }

        internal String classname = "";
        internal String funcname = "";

        private void InstrumentNode(CNode node)
        {
            if (!instrument || node.Token == null || node.Token.Filename.ToUpper() == "ccodecoverage.asp".ToUpper() ||
                node.Token.Filename.ToUpper() == "codecoveragereport.asp".ToUpper())
                return;

            int num = instrumentnumber.intValue();
            instrumentnumber.increment();

            visitor.InstrumentNode(node, num);

            instrumentdb.Write(node.Token.Filename + "," + classname + "," + funcname + "," + node.Token.LineNumber +
                               "," + num +
                               ",");
            bool printeddb = false;
            if ((dbtype & DbMsSql) != 0)
            {
                instrumentdb.Write("DbMsSql");
                printeddb = true;
            }
            if ((dbtype & DbAccess) != 0)
            {
                if (printeddb)
                    instrumentdb.Write("|");
                instrumentdb.Write("DbAccess");
                printeddb = true;
            }
            if ((dbtype & DbMySql) != 0)
            {
                if (printeddb)
                    instrumentdb.Write("|");
                instrumentdb.Write("DbMySql");
                printeddb = true;
            }
            instrumentdb.WriteLine();
        }

        public void EnterFunction(CFunction function)
        {
            foreach (var item in functionInterceptors)
                item.EnterFunction(function, this);
        }

        public void ExitFunction(CFunction function)
        {
            foreach (var item in functionInterceptors)
                item.ExitFunction(function, this);
        }

        public void VisitBlock(CStatementBlock statements)
        {
            visitor.VisitBlock(statements);
        }

        public void VisitAssignment(CAssignment assign)
        {
            InstrumentNode(assign);
            visitor.VisitAssignment(assign);
        }

        public void VisitCase(CCase ccase)
        {
            visitor.VisitCase(ccase);
        }

        public void VisitInterface(CInterface iface)
        {
            if (canGenerate(iface))
            {
                classname = iface.RawName;
                visitor.VisitInterface(iface);
                classname = "";
            }
        }

        public void VisitEnum(CEnum eration)
        {
            if (canGenerate(eration))
            {
                classname = eration.RawName;
                visitor.VisitEnum(eration);
                classname = "";
            }
        }

        public void VisitClass(CClass cclas)
        {
            if (canGenerate(cclas))
            {
                classname = cclas.RawName;
                visitor.VisitClass(cclas);
                classname = "";
            }
        }

        public void VisitComment(CComment comment)
        {
            visitor.VisitComment(comment);
        }

        public void VisitConcat(CConcat concat)
        {
            visitor.VisitConcat(concat);
        }

        public void VisitConst(CConst cconst)
        {
            if (canGenerate(cconst))
                visitor.VisitConst(cconst);
        }

        public void VisitDim(CDim dim)
        {
            if (classname == "" && funcname == "" && !canGenerate(dim))
                return;
            visitor.VisitDim(dim);
        }

        public void VisitDo(CDo cdo)
        {
            InstrumentNode(cdo);
            visitor.VisitDo(cdo);
        }

        public void VisitExit(CExit exit)
        {
            InstrumentNode(exit);
            visitor.VisitExit(exit);
        }

        public void VisitReturn(CReturn _return)
        {
            InstrumentNode(_return);
            visitor.VisitReturn(_return);
        }

        public void VisitFor(CFor cfor)
        {
            InstrumentNode(cfor);
            visitor.VisitFor(cfor);
        }

        public void VisitForEach(CForEach _foreach)
        {
            InstrumentNode(_foreach);
            visitor.VisitForEach(_foreach);
        }

        public void VisitFunction(CFunction function)
        {
            if (canGenerate(function))
            {
                funcname = function.Name;
                visitor.VisitFunction(function);
                funcname = "";
            }
        }

        public void VisitHtml(CHtml html)
        {
            visitor.VisitHtml(html);
        }

        internal class DbTypeInfo
        {
            internal int oldtype;
            internal int elsetype = -1;
            internal int currenttype;
        }

        public Object PreAcceptThen(CIf cif, Object objstate)
        {
            int newtype = 0;
            CExpression exp = cif.Condition;
            bool not = exp is CNot;
            if (not)
            {
                exp = ((CNot)exp).Operand;
                if (exp is CParenExpression)
                    exp = ((CParenExpression)exp).InnerExpression;
            }

            if (exp is CLogic && ((CLogic)exp).Operation.Value == "or")
            {
                CExpression lhs = ((CLogic)exp).Left;
                if (lhs is CAccess && ((CAccess)lhs).IsRootAccess &&
                    ((CAccess)lhs).ReferenceToken.Value == "g_fmssql")
                    newtype |= DbMsSql;
                exp = ((CLogic)exp).Right;
            }

            if (exp is CAccess)
            {
                CAccess acc = (CAccess)exp;
                if (acc.IsRootAccess)
                {
                    if (acc.ReferenceToken.Value == "g_fmssql")
                        newtype |= DbMsSql;
                    if (acc.ReferenceToken.Value == "g_fmysql")
                        newtype |= DbMySql;
                }
            }

            if (newtype == 0)
                return null;

            if (not)
                newtype = ~newtype;

            DbTypeInfo state;
            if (objstate == null)
            {
                state = new DbTypeInfo();
                state.oldtype = dbtype;
            }
            else
                state = (DbTypeInfo)objstate;
            state.currenttype = newtype;
            state.elsetype &= ~newtype;

            dbtype = newtype;
            return state;
        }

        public void PreAcceptElse(CIf cif, Object objstate)
        {
            if (objstate == null)
                return;

            DbTypeInfo state = (DbTypeInfo)objstate;
            dbtype = state.elsetype;
        }

        public void PreAcceptEndIf(CIf cif, Object objstate)
        {
            if (objstate == null)
                return;

            DbTypeInfo state = (DbTypeInfo)objstate;
            dbtype = state.oldtype;
        }

        public void VisitIf(CIf cif)
        {
            if (!cif.IsElseIf)
                InstrumentNode(cif);
            visitor.VisitIf(cif);
        }

        public void VisitMemberVariable(CMemberVariable membervariable)
        {
            visitor.VisitMemberVariable(membervariable);
        }

        public void VisitReDim(CReDim redim)
        {
            InstrumentNode(redim);
            visitor.VisitReDim(redim);
        }

        public void VisitSelect(CSelect select)
        {
            InstrumentNode(select);
            visitor.VisitSelect(select);
        }

        public void VisitSpecialEqual(CSpecialEqual specialequal)
        {
            InstrumentNode(specialequal);
            visitor.VisitSpecialEqual(specialequal);
        }

        public void VisitStatement(CStatement statement)
        {
            if (!(statement is CNewline))
                InstrumentNode(statement);
            visitor.VisitStatement(statement);
        }

        public void VisitToken(CToken token)
        {
            visitor.VisitToken(token);
        }

        public void VisitWhile(CWhile cwhile)
        {
            InstrumentNode(cwhile);
            visitor.VisitWhile(cwhile);
        }

        public void VisitWith(CWith with)
        {
            InstrumentNode(with);
            visitor.VisitWith(with);
        }

        internal void VisitExpression(CExpression exp)
        {
            exp.Accept(visitor);
        }

        public void VisitAccess(CAccess access)
        {
            VisitExpression(access);
        }

        public void VisitComparison(CComparison compare)
        {
            VisitExpression(compare);
        }

        public void VisitConstantExpression(CConstantExpression constant)
        {
            VisitExpression(constant);
        }

        public void VisitDefaultAccess(CDefaultAccess access)
        {
            VisitExpression(access);
        }

        public void VisitLogic(CLogic logic)
        {
            VisitExpression(logic);
        }

        public void VisitMath(CMath math)
        {
            VisitExpression(math);
        }

        public void VisitMathUnary(CMathUnary math)
        {
            VisitExpression(math);
        }

        public void VisitMemberAccess(CMemberAccess access)
        {
            VisitExpression(access);
        }

        public void VisitNew(CNew _new)
        {
            VisitExpression(_new);
        }

        public void VisitNot(CNot not)
        {
            VisitExpression(not);
        }

        public void VisitOnError(COnError onerror)
        {
            onerror.Accept(visitor);
        }

        public void VisitParameters(CParameters parameters)
        {
            visitor.VisitParameters(parameters);
        }

        public void VisitThisAccess(CThisAccess access)
        {
            VisitExpression(access);
        }

        public void VisitBaseAccess(CBaseAccess access)
        {
            VisitExpression(access);
        }

        public void VisitWithAccess(CWithAccess access)
        {
            VisitExpression(access);
        }

        public void VisitTernary(CTernary ternary)
        {
            VisitExpression(ternary);
        }


        public void VisitParenExpression(CParenExpression exp)
        {
            VisitExpression(exp);
        }

        public void VisitVariable(CVariable var)
        {
            visitor.VisitVariable(var);
        }

        public void VisitTry(CTry _try)
        {
            visitor.VisitTry(_try);
        }

        public void VisitCatch(CCatch _catch)
        {
            visitor.VisitCatch(_catch);
        }

        public void VisitFinally(CFinally _finally)
        {
            visitor.VisitFinally(_finally);
        }

        public void VisitThrow(CThrow _throw)
        {
            visitor.VisitThrow(_throw);
        }

        public void VisitPictureOf(CPictureOfExpression pic)
        {
            visitor.VisitPictureOf(pic);
        }

        public void VisitOption(COption option)
        {
            foreach (var item in this.functionInterceptors)
                item.Option(option, this);
            visitor.VisitOption(option);
        }

        public void VisitLambdaExpression(CLambdaExpression lambda)
        {
            visitor.VisitLambdaExpression(lambda);
        }

        public void VisitProgram(CProgram program)
        {
            visitor.VisitProgram(program);
        }

        internal CToken CreateToken(String tokenSource, TokenTypes type)
        {
            return CreateToken(tokenSource, tokenSource.ToLower(), type, null);
        }

        internal CToken CreateToken(String tokenSource, string rawValue, TokenTypes type)
        {
            return CreateToken(tokenSource, rawValue, type, null);
        }

        internal CToken CreateTokenWithAdditionalInfo(String tokenSource, TokenTypes type)
        {
            return CreateTokenWithAdditionalInfo(tokenSource, tokenSource, type);
        }

        internal CToken filenamesrc = null;

        internal CToken CreateTokenWithAdditionalInfo(String rawvalue, String sValue, TokenTypes type)
        {
            return CreateToken(rawvalue, sValue, type, rawvalue);
        }

        internal CToken CreateToken(String rawvalue, String sValue, TokenTypes type, object additionalInfo)
        {
            String filename = filenamesrc == null ? null : filenamesrc.Filename;

            CToken result = new CToken(filename, -1, 0, "", type, sValue, rawvalue, false);
            result.AdditionalInfo = additionalInfo;

            return result;
        }

        public void VisitFile(CFile file)
        {
            if (!canGenerate(file))
                return;

            visitor.PreVisitFile(file);

            filenamesrc = file.Token;

            new CNewline(null).Accept(visitor);

            if (instrument)
            {
                COption cCodeCoverage =
                    new COption(new CToken(file.Filename, TokenTypes.keyword, "option"),
                                new CToken(file.Filename, TokenTypes.keyword, "include"),
                                new CToken(file.Filename, TokenTypes.str, "cCodeCoverage.asp"));
                cCodeCoverage.Accept(this);
            }

            visitor.VisitFile(file);

            if (file.Attributes.contains("GenerateProcessAjaxFunction"))
            {
                CFunction unicoderequest = CProgram.Global.FindFunction("unicoderequest");
                CFunction intrequest = CProgram.Global.FindFunction("intrequest");
                CFunction boolrequest = CProgram.Global.FindFunction("boolrequest");

                CToken tok = CreateTokenWithAdditionalInfo("ProcessAjax", "processajax", TokenTypes.identifier);
                CFunction processAjax =
                    new CFunction(tok, tok.RawValue, tok.Value, TokenTypes.visPublic, CFunction.vbSub, null,
                                  new CTypeRef(null, BuiltIns.Void));

                CAccess pivotitem = new CAccess(unicoderequest.Token, unicoderequest.Token);
                pivotitem.ReferenceTarget = unicoderequest;
                CParameters pivotparams = new CParameters();
                pivotparams.Unnamed.Add(
                    new CConstantExpression(CreateTokenWithAdditionalInfo("sFunction", TokenTypes.str)));
                CSelect select =
                    new CSelect(CreateTokenWithAdditionalInfo("select", TokenTypes.controlFlow),
                                new CDefaultAccess(pivotitem.Token, pivotitem, pivotparams));

                IEnumerator it = CProgram.Global.Functions.GetEnumerator();
                List<CVariable> vars = new List<CVariable>();
                while (it.MoveNext())
                {
                    CFunction func = (CFunction)it.Current;
                    if (!func.Attributes.contains("ExecuteOnServer"))
                        continue;

                    CStatementBlock block = new CStatementBlock();
                    CParameters funcParams = new CParameters();
                    for (int ixArg = 0; ixArg < func.Arguments.Count - 3; ixArg++)
                    {
                        CArgument arg = func.Arguments[ixArg];

                        tok =
                            CreateTokenWithAdditionalInfo("vParam" + (ixArg + 1), "vparam" + (ixArg + 1),
                                                          TokenTypes.identifier);
                        if (ixArg >= vars.Count)
                            vars.Add(new CVariable(tok, false, new CTypeRef(null, BuiltIns.Variant), null, null, null));

                        CAssignment assign = new CAssignment(tok);

                        CAccess left = new CAccess(tok, tok);
                        left.ReferenceTarget = vars[ixArg];
                        assign.Target = left;
                        funcParams.Unnamed.Add(left);

                        CFunction rightfunc = intrequest;
                        if (arg.Type.RawName == "Boolean")
                            rightfunc = boolrequest;
                        else if (arg.Type.RawName == "String")
                            rightfunc = unicoderequest;

                        CParameters parameters = new CParameters();
                        parameters.Unnamed.Add(
                            new CConstantExpression(CreateTokenWithAdditionalInfo(tok.RawValue, TokenTypes.str)));

                        CAccess rightitem = new CAccess(rightfunc.Token, rightfunc.Token);
                        rightitem.ReferenceTarget = rightfunc;

                        assign.Source = new CDefaultAccess(rightfunc.Token, rightitem, parameters);
                        assign.Source.RhsAssignmentSource = true;

                        block.Add(assign);
                    }

                    CAccess calledItem = new CAccess(func.Token, func.Token);
                    CDefaultAccess called = new CDefaultAccess(func.Token, calledItem, funcParams);
                    calledItem.ReferenceTarget = calledItem.ReferenceTarget = func;

                    if (func.Attributes.contains("picture") || func.FunctionType == CFunction.vbFunction)
                    {
                        CAccess returnItem = new CAccess(processAjax.Token, processAjax.Token);
                        returnItem.ReferenceTarget = processAjax;

                        CAssignment assign = new CAssignment(processAjax.Token);
                        assign.Target = returnItem;

                        if (func.FunctionType == CFunction.vbFunction)
                            assign.Source = called;
                        else
                            assign.Source = new CPictureOfExpression(tok, called);

                        block.Add(assign);
                    }
                    else
                        block.Add(new CExpressionStatement(called));

                    tok = CreateTokenWithAdditionalInfo("case", TokenTypes.controlFlow);
                    CExpression exp =
                        new CConstantExpression(CreateTokenWithAdditionalInfo(func.RawName, TokenTypes.str));
                    select.Cases.Add(new CCase(tok, exp, block));
                }

                CDim dim = new CDim(CreateTokenWithAdditionalInfo("Dim", TokenTypes.declaration));
                dim.Variables.AddRange(vars);
                processAjax.Statements.Add(dim);
                processAjax.Statements.Add(select);

                CNewline nl = new CNewline(CreateTokenWithAdditionalInfo("\n", TokenTypes.newline));
                nl.Accept(this);
                processAjax.Accept(this);
                nl.Accept(this);
            }
        }

        public void VisitDirective(CDirective directive)
        {
            visitor.VisitDirective(directive);
        }

        public bool canGenerate(CFile file)
        {
            if (file.Attributes.contains("ExecuteAtCompiler") || file.IncludedByExecuteAtCompilerFile)
                return false;

            return internalCanGenerate(file);
        }

        protected internal virtual bool internalCanGenerate(CFile file)
        {
            return true;
        }

        public bool canGenerate(CDim dim)
        {
            if (dim.Variables.Count != 0 && dim.Variables[0].Attributes.contains("ExecuteAtCompiler"))
                return false;

            return internalCanGenerate(dim);
        }

        protected internal virtual bool internalCanGenerate(CDim dim)
        {
            if (dim.Variables.Count != 0 && dim.Variables[0].Attributes.contains("ExecuteOnClient"))
                return false;
            return true;
        }

        public bool canGenerate(CFunction function)
        {
            if (function.Attributes.contains("ExecuteAtCompiler") ||
                (function.Class != null && function.Class.Attributes.contains("ExecuteAtCompiler")))
                return false;

            return internalCanGenerate(function);
        }

        protected internal virtual bool internalCanGenerate(CFunction function)
        {
            if (function.Attributes.contains("ExecuteOnClient"))
                return false;
            return true;
        }

        public bool canGenerate(CClass _class)
        {
            if (_class.Attributes.contains("ExecuteAtCompiler"))
                return false;

            return internalCanGenerate(_class);
        }

        protected internal virtual bool internalCanGenerate(CClass _class)
        {
            if (_class.Attributes.contains("ExecuteOnClient"))
                return false;
            return true;
        }

        public bool canGenerate(CConst _class)
        {
            if (_class.Attributes.contains("ExecuteAtCompiler"))
                return false;

            return internalCanGenerate(_class);
        }

        protected internal virtual bool internalCanGenerate(CConst _class)
        {
            if (_class.Attributes.contains("ExecuteOnClient"))
                return false;
            return true;
        }


        private static Regex startTrim = new Regex(@"^[\s\r\n]+");
        private static Regex endTrim = new Regex(@"[\s\r\n]+$");
        private static Regex interHTMLnewlines = new Regex(@"\>\s*\r?\n(\s*\r?\n)*\s*\<");
        private static Regex endSelect = new Regex(@"(<\s*\/\s*select\s*>)");
        private static Regex newlines = new Regex(@"\s*\r?\n(\s*\r?\n)*\s*");

        public static string CleanHtml(CHtml html, ref bool startNL, ref bool endNL)
        {
            bool _start = startNL, _end = endNL;
            String s = startTrim.Replace(html.HtmlString, delegate(Match m)
            {
                _start = m.Value.Contains(("\n"));
                return " ";
            });
            s = endTrim.Replace(s, delegate(Match m)
            {
                _end = m.Value.Contains(("\n"));
                return " ";
            });
            s = interHTMLnewlines.Replace(s, "> <");
            s = newlines.Replace(s, "\n");
            s = s.Replace("\r\n", "\n");

            startNL = _start;
            endNL = _end;
            return s;
        }


        public void VisitLock(CLock @lock)
        {
            @lock.Accept(visitor);
        }

        public void VisitOnExit(COnExit onexit)
        {
            onexit.Accept(visitor);
        }

        public void VisitOptionalByRef(COptionalByRef optbyref)
        {
            optbyref.Accept(visitor);
        }

        public void VisitAttribute(CAttribute attr)
        {
            attr.Accept(visitor);
        }

        public void VisitGlobalAccess(CGlobalAccess cga)
        {
            cga.Accept(visitor);
        }
    }
}
