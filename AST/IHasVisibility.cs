namespace FogCreek.Wasabi.AST
{
    public interface IHasVisibility
    {
        CClass DeclaringClass { get; }
        TokenTypes Visibility { get; }
        bool IsStatic { get; }
    }
}
