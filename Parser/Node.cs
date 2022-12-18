using System.Data.Common;
using System.Diagnostics.Tracing;
using Scanner;

namespace Parser;

public abstract class SyntaxNode
{
    protected SyntaxNode(Token token)
    {
        Token = token;
    }

    protected Token Token { get; }

    protected static void DrawIndent(TextWriter writer, int depth)
    {
        for (var i = 0; i < depth + 1; ++i) writer.Write("   ");
    }

    protected static void DrawList(TextWriter writer, int depth, List<SyntaxNode> list)
    {
        foreach (var t in list)
        {
            t.Draw(writer, depth);
        }
    }

    protected static void WriteWithIndent(TextWriter writer, int depth, string s)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(s);
    }

    public abstract void Draw(TextWriter writer, int depth);
}

public class NodeUnOp : SyntaxNode
{
    private SyntaxNode Operand { get; }

    public NodeUnOp(Token token, SyntaxNode operand) : base(token)
    {
        Operand = operand;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token?.Raw);
        DrawIndent(writer, depth + 1);
        writer.WriteLine(Operand);
    }
}

public class NodeVarRef : SyntaxNode
{
    public NodeVarRef(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        throw new NotImplementedException();
    }
}

public class NodeRelOp : SyntaxNode
{
    private SyntaxNode Left { get; }
    private SyntaxNode Right { get; }

    public NodeRelOp(Token token, SyntaxNode left, SyntaxNode right) : base(token)
    {
        Left = left;
        Right = right;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Raw);
        Left.Draw(writer, depth + 1);
        Right.Draw(writer, depth + 1);
    }
}

public class NodeBinaryOp : SyntaxNode
{
    private SyntaxNode Left { get; }
    private SyntaxNode Right { get; }

    public NodeBinaryOp(Token token, SyntaxNode left, SyntaxNode right) : base(token)
    {
        Left = left;
        Right = right;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Raw);
        Left.Draw(writer, depth + 1);
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
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Value);
    }
}

public class NodeString : SyntaxNode
{
    public NodeString(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Value);
    }
}

public class NodeVariable : NodeVarRef
{
    public NodeVariable(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Value);
    }
}

public class NodeRange : SyntaxNode
{
    private SyntaxNode Left { get; }
    private SyntaxNode Right { get; }

    public NodeRange(SyntaxNode left, SyntaxNode right) : base(null)
    {
        Left = left;
        Right = right;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("range");
        Left.Draw(writer, depth + 1);
        Right.Draw(writer, depth + 1);
    }
}

public class NodeArrayAccess : NodeVarRef
{
    private SyntaxNode VarRef { get; }
    private List<SyntaxNode> Indexes { get; }

    public NodeArrayAccess(SyntaxNode varRef, List<SyntaxNode> indexes) : base(null)
    {
        VarRef = varRef;
        Indexes = indexes;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("array access");
        VarRef.Draw(writer, depth + 1);
        DrawList(writer, depth + 1, Indexes);
    }
}

public class NodeCall : NodeVarRef
{
    private SyntaxNode VarRef { get; }
    private List<SyntaxNode> Params { get; }

    public NodeCall(SyntaxNode varRef, List<SyntaxNode> @params) : base(default)
    {
        VarRef = varRef;
        Params = @params;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("call");
        VarRef.Draw(writer, depth + 1);
        DrawList(writer, depth + 1, Params);
    }
}

public class NodeRecordAccess : NodeVarRef
{
    private SyntaxNode VarRef { get; }
    private SyntaxNode Field { get; }

    public NodeRecordAccess(SyntaxNode varRef, SyntaxNode field) : base(default)
    {
        VarRef = varRef;
        Field = field;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("record access");
        VarRef.Draw(writer, depth + 1);
        Field.Draw(writer, depth + 1);
    }
}

public class NodePrimitiveType : SyntaxNode
{
    public NodePrimitiveType(Token token) : base(token)
    {
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine($"{Token.Raw}");
    }
}

public class NodeArrayType : SyntaxNode
{
    private List<SyntaxNode> Ranges { get; }
    private SyntaxNode Type { get; }

    public NodeArrayType(List<SyntaxNode> ranges, SyntaxNode type) : base(default)
    {
        Ranges = ranges;
        Type = type;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("array");
        DrawList(writer, depth + 1, Ranges);
        Type.Draw(writer, depth + 1);
    }
}

public class RecordField : SyntaxNode
{
    private SyntaxNode Id { get; }
    private SyntaxNode Type { get; }

    public RecordField(SyntaxNode id, SyntaxNode type) : base(default)
    {
        Id = id;
        Type = type;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("field");
        Id.Draw(writer, depth + 1);
        Type.Draw(writer, depth + 1);
    }
}

public class NodeRecordType : SyntaxNode
{
    private List<SyntaxNode> Fields { get; }

    public NodeRecordType(List<SyntaxNode> fields) : base(default)
    {
        Fields = fields;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("record");
        DrawList(writer, depth + 1, Fields);
    }
}

public abstract class NodeStatement : SyntaxNode
{
    protected NodeStatement(Token token) : base(token)
    {
    }
}

public class NodeAssigmentStatement : SyntaxNode
{
    private SyntaxNode VarRef { get; }
    private SyntaxNode Exp { get; }

    public NodeAssigmentStatement(Token token, SyntaxNode varRef, SyntaxNode exp) : base(token)
    {
        VarRef = varRef;
        Exp = exp;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine(Token.Raw);
        VarRef.Draw(writer, depth + 1);
        Exp.Draw(writer, depth + 1);
    }
}

public class NodeBeginStatement : SyntaxNode
{
    private List<SyntaxNode> Statements { get; }

    public NodeBeginStatement(List<SyntaxNode> statements) : base(default)
    {
        Statements = statements;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("begin");
        DrawList(writer, depth + 1, Statements);
    }
}

public class NodeIfStatement : SyntaxNode
{
    private SyntaxNode Expression { get; }
    private SyntaxNode Statement { get; }
    private SyntaxNode? ElseStatement { get; }

    public NodeIfStatement(SyntaxNode expression, SyntaxNode statement, SyntaxNode elseStatement) : base(default)
    {
        Expression = expression;
        Statement = statement;
        ElseStatement = elseStatement;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("if");
        DrawIndent(writer, depth + 1);
        writer.WriteLine("exp:");
        Expression.Draw(writer, depth + 2);
        DrawIndent(writer, depth + 1);
        writer.WriteLine("body:");
        Statement.Draw(writer, depth + 2);
        DrawIndent(writer, depth + 1);
        writer.WriteLine("else:");
        ElseStatement?.Draw(writer, depth + 2);
    }
}

public class NodeForStatement : SyntaxNode
{
    private SyntaxNode Id { get; }
    private SyntaxNode BeginExpression { get; }
    private Token ToOrDownTo { get; }
    private SyntaxNode EndExpression { get; }
    private SyntaxNode Statement { get; }

    public NodeForStatement(SyntaxNode id, SyntaxNode beginExpression, Token toOrDownTo, SyntaxNode endExpression,
        SyntaxNode statement) : base(default)
    {
        Id = id;
        BeginExpression = beginExpression;
        ToOrDownTo = toOrDownTo;
        EndExpression = endExpression;
        Statement = statement;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("for");
        Id.Draw(writer, depth + 1);
        BeginExpression.Draw(writer, depth + 1);
        DrawIndent(writer, depth + 1);
        writer.WriteLine(ToOrDownTo.Value);
        Statement.Draw(writer, depth + 1);
    }
}

public class NodeWhileStatement : SyntaxNode
{
    private SyntaxNode Expression { get; }
    private SyntaxNode Statement { get; }

    public NodeWhileStatement(SyntaxNode expression,
        SyntaxNode statement) : base(default)
    {
        Expression = expression;
        Statement = statement;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("while");
        Expression.Draw(writer, depth + 1);
        Statement.Draw(writer, depth + 1);
    }
}

public class NodeTypeDeclaration : SyntaxNode
{
    public SyntaxNode Id { get; }
    public SyntaxNode Type { get; }

    public NodeTypeDeclaration(SyntaxNode id, SyntaxNode type) : base(default)
    {
        Id = id;
        Type = type;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("type declaration");
        Id.Draw(writer, depth + 1);
        Type.Draw(writer, depth + 1);
    }
}

public class NodeProgram : SyntaxNode
{
    private List<SyntaxNode> Declarations { get; }
    private SyntaxNode Begin { get; }

    public NodeProgram(List<SyntaxNode> declarationses, SyntaxNode begin) : base(default)
    {
        Declarations = declarationses;
        Begin = begin;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("program");
        DrawList(writer, depth + 1, Declarations);
        Begin.Draw(writer, depth + 1);
    }
}

public class NodeVariableDeclaration : SyntaxNode
{
    public SyntaxNode Id { get; }
    public SyntaxNode Type { get; }
    public SyntaxNode? Value { get; }

    public NodeVariableDeclaration(SyntaxNode id, SyntaxNode type, SyntaxNode? value) : base(default)
    {
        Id = id;
        Type = type;
        Value = value;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("variable");
        Id.Draw(writer, depth + 1);
        Type.Draw(writer, depth + 1);
        Value?.Draw(writer, depth + 1);
    }
}

public class NodeConstantDeclaration : SyntaxNode
{
    public SyntaxNode Id { get; }
    public SyntaxNode? Type { get; }
    public SyntaxNode Value { get; }

    public NodeConstantDeclaration(SyntaxNode id, SyntaxNode? type, SyntaxNode value) : base(default)
    {
        Id = id;
        Type = type;
        Value = value;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        DrawIndent(writer, depth);
        writer.WriteLine("constant");
        Id.Draw(writer, depth + 1);
        Type?.Draw(writer, depth + 1);
        Value.Draw(writer, depth + 1);
    }
}

public class NodeParameterDeclaration : SyntaxNode
{
    private SyntaxNode? Modifier { get; }
    private SyntaxNode Id { get; }
    private SyntaxNode Type { get; }

    public NodeParameterDeclaration(SyntaxNode? mod, SyntaxNode id, SyntaxNode type) : base(default)
    {
        Modifier = mod;
        Id = id;
        Type = type;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        Modifier?.Draw(writer, depth);
        Id.Draw(writer, depth);
        Type.Draw(writer, depth);
    }
}

public class NodeProcedureDeclaration : SyntaxNode
{
    public SyntaxNode Id { get; }
    public List<SyntaxNode> Params { get; }
    public List<SyntaxNode> Declarations { get; }
    public SyntaxNode Begin { get; }

    public NodeProcedureDeclaration(SyntaxNode id, List<SyntaxNode> @params, List<SyntaxNode> declarations,
        SyntaxNode begin) : base(default)
    {
        Id = id;
        Params = @params;
        Declarations = declarations;
        Begin = begin;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        Id.Draw(writer, depth);
        WriteWithIndent(writer, depth + 1, "parameters:");
        DrawList(writer, depth + 2, Params);
        WriteWithIndent(writer, depth + 1, "declarations:");
        DrawList(writer, depth + 2, Declarations);
        Begin.Draw(writer, depth + 1);
    }
}

public class NodeFunctionDeclaration : SyntaxNode
{
    public SyntaxNode Id { get; }
    public List<SyntaxNode> Params { get; }
    public List<SyntaxNode> Declarations { get; }
    public SyntaxNode Begin { get; }
    public SyntaxNode Type { get; }

    public NodeFunctionDeclaration(SyntaxNode id, List<SyntaxNode> @params, List<SyntaxNode> declarations, SyntaxNode begin,
        SyntaxNode type) : base(default)
    {
        Id = id;
        Params = @params;
        Declarations = declarations;
        Begin = begin;
        Type = type;
    }

    public override void Draw(TextWriter writer, int depth)
    {
        Id.Draw(writer, depth);
        WriteWithIndent(writer, depth + 1, "parameters:");
        DrawList(writer, depth + 2, Params);
        WriteWithIndent(writer, depth + 1, "declarations:");
        DrawList(writer, depth + 2, Declarations);
        WriteWithIndent(writer, depth + 1, "type:");
        Type.Draw(writer, depth + 2);
        Begin.Draw(writer, depth + 1);
    }
}