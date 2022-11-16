using Scanner;

namespace Parser;

public abstract class SyntaxNode
{
    protected SyntaxNode(Token token)
    {
        Token = token;
    }

    public Token Token { get; }

    public abstract void Draw(TextWriter writer, int depth);
}

public class NodeBinaryOp : SyntaxNode
{
    public SyntaxNode Left { get; }
    public SyntaxNode Right { get; }

    public NodeBinaryOp(Token token, SyntaxNode left, SyntaxNode right) : base(token)
    {
        Left = left;
        Right = right;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        writer.WriteLine(Token.Raw);
        for (var i = 0; i < depth + 1; ++i) writer.Write("   ");
        Left.Draw(writer, depth + 1);
        writer.WriteLine();
        for (var i = 0; i < depth + 1; ++i) writer.Write("   ");
        Right.Draw(writer, depth + 1);
    }
}

public class NodeNumber : SyntaxNode
{
    public NodeNumber(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        writer.Write(Token.Value);
    }
}

public class NodeVariable : SyntaxNode
{
    public NodeVariable(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        writer.Write(Token.Value);
    }
}