
namespace FogCreek.Wasabi.AST
{
    public interface IVisitor
    {
        void VisitBlock(CStatementBlock statements);
        void VisitAssignment(CAssignment assign);
        void VisitCase(CCase ccase);
        void VisitClass(CClass cclas);
        void VisitComment(CComment comment);
        void VisitConcat(CConcat concat);
        void VisitConst(CConst cconst);
        void VisitDim(CDim dim);
        void VisitDo(CDo cdo);
        void VisitExit(CExit exit);
        void VisitReturn(CReturn _return);
        void VisitParenExpression(CParenExpression exp);
        void VisitFor(CFor cfor);
        void VisitForEach(CForEach _foreach);
        void VisitFunction(CFunction function);
        void VisitHtml(CHtml html);
        void VisitIf(CIf cif);
        void VisitMemberVariable(CMemberVariable membervariable);
        void VisitReDim(CReDim redim);
        void VisitSelect(CSelect select);
        void VisitSpecialEqual(CSpecialEqual specialequal);
        void VisitStatement(CStatement statement);
        void VisitToken(CToken token);
        void VisitWhile(CWhile cwhile);
        void VisitWith(CWith with);

        void VisitAccess(CAccess access);
        void VisitComparison(CComparison compare);
        void VisitConstantExpression(CConstantExpression constant);
        void VisitDefaultAccess(CDefaultAccess access);
        void VisitLogic(CLogic logic);
        void VisitMath(CMath math);
        void VisitMathUnary(CMathUnary math);
        void VisitMemberAccess(CMemberAccess access);
        void VisitNew(CNew _new);
        void VisitNot(CNot not);
        void VisitOnError(COnError onerror);
        void VisitParameters(CParameters parameters);
        void VisitThisAccess(CThisAccess access);
        void VisitWithAccess(CWithAccess access);
        void VisitTernary(CTernary ternary);

        void VisitVariable(CVariable var);
        void VisitPictureOf(CPictureOfExpression pic);
        void VisitOption(COption option);

        void VisitLambdaExpression(CLambdaExpression lambda);

        void VisitProgram(CProgram program);
        void VisitFile(CFile file);
        void VisitDirective(CDirective directive);

        void VisitInterface(CInterface iface);
        void VisitEnum(CEnum eration);

        void VisitTry(CTry _try);
        void VisitCatch(CCatch _catch);
        void VisitFinally(CFinally _finally);
        void VisitThrow(CThrow _throw);

        void VisitLock(CLock @lock);
        void VisitOnExit(COnExit onexit);
        void VisitBaseAccess(CBaseAccess _base);
        void VisitGlobalAccess(CGlobalAccess cGlobalAccess);
        void VisitOptionalByRef(COptionalByRef cOptionalByRef);
        void VisitAttribute(CAttribute cAttribute);
    }
}