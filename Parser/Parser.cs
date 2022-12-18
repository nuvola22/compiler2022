using System.Data;
using System.Reflection;
using Microsoft.VisualBasic.CompilerServices;
using Scanner;

namespace Parser;

public class ParserException : Exception
{
    public ParserException(Position pos, string message)
        : base($"{pos}, {message}")
    {
    }
}

public class Parser
{
    private readonly Scanner.Scanner _scanner;
    private Token? _currentToken;

    public Parser(Scanner.Scanner scanner)
    {
        _scanner = scanner;
        _currentToken = scanner.GetToken();
    }

    public SyntaxNode Expression()
    {
        var left = SimpleExpression();
        var token = _currentToken;
        
        while (token.Equals(Operation.Eq) || token.Equals(Operation.Neq) || token.Equals(Operation.Lt) ||
               token.Equals(Operation.Gt) || token.Equals(Operation.Lte) || token.Equals(Operation.Gte))
        {
            _currentToken = _scanner.GetToken();
            left = new NodeRelOp(token, left, SimpleExpression());
            token = _currentToken;
        }

        return left;
    }

    private SyntaxNode SimpleExpression()
    {
        var left = Term();
        var token = _currentToken;

        while (token.Equals(Operation.Add) || token.Equals(Operation.Sub) || token.Equals(Keywords.Or) ||
               token.Equals(Keywords.Xor))
        {
            _currentToken = _scanner.GetToken();
            left = new NodeBinaryOp(token, left, Term());
            token = _currentToken;
        }

        return left;
    }

    private SyntaxNode Term()
    {
        var left = SimpleTerm();
        var token = _currentToken;
        while (token.Equals(Operation.Mul) || token.Equals(Operation.Div) || token.Equals(Keywords.Div) ||
               token.Equals(Keywords.And) || token.Equals(Keywords.Shl) || token.Equals(Keywords.Shr))
        {
            _currentToken = _scanner.GetToken();
            left = new NodeBinaryOp(token, left, SimpleTerm());
            token = _currentToken;
        }

        return left;
    }

    private SyntaxNode SimpleTerm()
    {
        if (!(_currentToken.Equals(Operation.Add) || _currentToken.Equals(Operation.Sub) ||
              _currentToken.Equals(Keywords.Not))) return Factor();
        var op = _currentToken;
        _currentToken = _scanner.GetToken();
        return new NodeUnOp(op, SimpleTerm());
    }

    private SyntaxNode Factor()
    {
        var token = _currentToken;

        if (token.Equals(TokenType.LitInt) || token.Equals(TokenType.LitDob))
        {
            _currentToken = _scanner.GetToken();
            return new NodeNumber(token);
        }

        if (token.Equals(TokenType.Id))
        {
            return VarRef(Id());
        }

        if (token.Equals(TokenType.LitStr))
        {
            _currentToken = _scanner.GetToken();
            return new NodeString(token);
        }

        if (token.Equals(Separator.LPar))
        {
            _currentToken = _scanner.GetToken();
            var expression = Expression();

            if (!token.Equals(Separator.RPar))
            {
                Console.WriteLine(token);
                throw new ParserException(_currentToken.Pos, "Expected )");
            }

            _currentToken = _scanner.GetToken();
            return expression;
        }

        throw new ParserException(token.Pos, "Factor expected");
    }

    private SyntaxNode VarRef(SyntaxNode varRef)
    {
        var left = varRef;
        // a[1]
        // left = varRef - a
        // left = ArrayAccess(a, indexes)
        // ()
        // []
        // .
        
        // a[1,2](hello).hello1
        
        //                                            field access
        //                                          /           \
        //                                  call access         hello1
        //                              /             \
        //                       array access          hello
        //                      /           \
        //                     a              [1,2]
        //               
        while (true)
        {
            if (_currentToken.Equals(Separator.LBra))
            {
                _currentToken = _scanner.GetToken();
                var indexes = ExpressionList(sep: Separator.Comma, sep_end: Separator.RBra);
                if (indexes.Count == 0)
                {
                    throw new ParserException(_currentToken.Pos, "Expression expected");
                }
                Require(Separator.RBra);
                _currentToken = _scanner.GetToken();
                left = new NodeArrayAccess(left, indexes);
            }
            else if (_currentToken.Equals(Separator.LPar))
            {
                _currentToken = _scanner.GetToken();
                var @params = ExpressionList(sep: Separator.Comma);
                Require(Separator.RPar);
                _currentToken = _scanner.GetToken();
                left = new NodeCall(left, @params);
            }
            else if (_currentToken.Equals(Separator.Dot))
            {
                _currentToken = _scanner.GetToken();
                var field = Id();
                left = new NodeRecordAccess(left, field);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private List<SyntaxNode> ExpressionList(Separator sep = Separator.Comma,
        Separator sep_end = Separator.RPar)
    {
        var res = new List<SyntaxNode>();
        if (_currentToken.Equals(sep_end))
        {
            return res;
        }
        
        res.Add(Expression());
        while (_currentToken.Equals(sep))
        {
            _currentToken = _scanner.GetToken();
            res.Add(Expression());
        }

        return res;
    }

    private List<SyntaxNode> IdList(Separator sep = Separator.Comma,
        Separator sep_end = Separator.RPar)
    {
        var res = new List<SyntaxNode>();
        if (_currentToken.Equals(sep_end))
        {
            return res;
        }

        res.Add(Id());
        while (_currentToken.Equals(sep))
        {
            _currentToken = _scanner.GetToken();
            res.Add(Id());
        }

        return res;
    }

    private SyntaxNode Id()
    {
        Require(TokenType.Id);
        var id = new NodeVariable(_currentToken);
        _currentToken = _scanner.GetToken();
        return id;
    }

    public SyntaxNode Type()
    {
        // var a: string;
        if (_currentToken.Equals(TokenType.Id) || _currentToken.Equals(Keywords.String))
        {
            var t = _currentToken;
            _currentToken = _scanner.GetToken();
            return new NodePrimitiveType(t);
        }

        if (_currentToken.Equals(Keywords.Array))
        {
            _currentToken = _scanner.GetToken();
            return TypeArray();
        }

        if (_currentToken.Equals(Keywords.Record))
        {
            _currentToken = _scanner.GetToken();
            return RecordType();
        }

        throw new ParserException(_currentToken.Pos, "type expected");
    }

    private List<SyntaxNode> Fields()
    {
        var ids = IdList();
        Require(Separator.Col);
        _currentToken = _scanner.GetToken();
        var type = Type();
        return ids.Select(id => new RecordField(id, type)).Cast<SyntaxNode>().ToList();
    }

    private SyntaxNode RecordType()
    {
        if (_currentToken.Equals(Keywords.End))
        {
            return new NodeRecordType(new List<SyntaxNode>());
        }
        var fields = Fields();
        while (_currentToken.Equals(Separator.Sem))
        {
            _currentToken = _scanner.GetToken();
            if (_currentToken.Equals(Keywords.End))
            {
                break;
            }
            // a = {1,2,3,4,5}
            // a = a.concat({6,7,8})
            // {1,2,3,4,5, 6,7,8}
            fields = fields.Concat(Fields()).ToList();
        }

        Require(Keywords.End);
        _currentToken = _scanner.GetToken();
        return new NodeRecordType(fields);
    }

    private SyntaxNode TypeArray()
    {
        // array[1..3] of array[4..5] of integer;
        
        // left(range), right(type)
        //
        //      array
        //      /      \
        //     1..3       array
        //              /      \
        //             4..5     integer
        Require(Separator.LBra);
        _currentToken = _scanner.GetToken();
        var ranges = new List<SyntaxNode> { Range() };
        while (_currentToken.Equals(Separator.Comma))
        {
            _currentToken = _scanner.GetToken();
            ranges.Add(Range());
        }
        Require(Separator.RBra);
        _currentToken = _scanner.GetToken();
        Require(Keywords.Of);
        _currentToken = _scanner.GetToken();
        var type = Type();
        return new NodeArrayType(ranges, type);
    }

    private SyntaxNode Range()
    {
        var beg = Expression();
        Require(Separator.Range);
        _currentToken = _scanner.GetToken();
        var end = Expression();
        return new NodeRange(beg, end);
    }

    public SyntaxNode SimpleStatement()
    {
        // a() += 10;
        // a()
        //  call
        //  / \
        // a    empty params
        // a += 10;
        // a
        // assigment statement
        //     /    |       \
        //    a     :=      10
        
        var varRef = Expression();
        if (varRef is NodeCall)
        {
            return varRef;
        }

        if (varRef is NodeVariable or NodeArrayAccess or NodeArrayAccess or NodeRecordAccess)
        {
            if (!(_currentToken.Equals(Operation.Asg) || _currentToken.Equals(Operation.AddAsg) ||
                  _currentToken.Equals(Operation.SubAsg) || _currentToken.Equals(Operation.MulAsg) ||
                  _currentToken.Equals(Operation.DivAsg)))
            {
                throw new ParserException(_currentToken.Pos, "Assigment operation expected");
            }

            var asg = _currentToken;
            _currentToken = _scanner.GetToken();
            var exp = Expression();
            return new NodeAssigmentStatement(asg, varRef, exp);
        }

        throw new ParserException(_currentToken.Pos, "Illegal expression");
    }

    public SyntaxNode BeginStatement()
    {
        var statements = new List<SyntaxNode>();
        while (!_currentToken.Equals(Keywords.End))
        {
            statements.Add(Statement());
            Require(Separator.Sem);
            _currentToken = _scanner.GetToken();
        }

        Require(Keywords.End);
        _currentToken = _scanner.GetToken();

        return new NodeBeginStatement(statements);
    }

    private SyntaxNode IfStatement()
    {
        var expression = Expression();
        Require(Keywords.Then);
        _currentToken = _scanner.GetToken();
        var statement = Statement();
        SyntaxNode? elseStatement = null;
        if (_currentToken.Equals(Keywords.Else))
        {
            _currentToken = _scanner.GetToken();
            elseStatement = Statement();
        }

        return new NodeIfStatement(expression, statement, elseStatement);
    }

    private SyntaxNode WhileStatement()
    {
        var expression = Expression();
        Require(Keywords.Do);
        _currentToken = _scanner.GetToken();
        var statement = Statement();
        return new NodeWhileStatement(expression, statement);
    }

    private SyntaxNode ForStatement()
    {
        var id = Id();
        Require(Operation.Asg);
        _currentToken = _scanner.GetToken();
        var beg = Expression();
        Require(Keywords.To, Keywords.Downto);
        var toOrDownTo = _currentToken;
        _currentToken = _scanner.GetToken();
        var end = Expression();
        Require(Keywords.Do);
        _currentToken = _scanner.GetToken();
        var statement = Statement();
        return new NodeForStatement(id, beg, toOrDownTo, end, statement);
    }

    private SyntaxNode Statement()
    {
        if (_currentToken.Equals(Keywords.Begin))
        {
            _currentToken = _scanner.GetToken();
            return BeginStatement();
        }

        if (_currentToken.Equals(Keywords.If))
        {
            _currentToken = _scanner.GetToken();
            return IfStatement();
        }

        if (_currentToken.Equals(Keywords.For))
        {
            _currentToken = _scanner.GetToken();
            return ForStatement();
        }

        if (_currentToken.Equals(Keywords.While))
        {
            _currentToken = _scanner.GetToken();
            return WhileStatement();
        }

        return SimpleStatement();
    }

    private void Require(Separator sep)
    {
        if (_currentToken != null && !_currentToken.Equals(sep))
        {
            throw new ParserException(_currentToken.Pos, $"{sep} expected");
        }
    }

    private void Require(Operation op)
    {
        if (_currentToken != null && !_currentToken.Equals(op))
        {
            throw new ParserException(_currentToken.Pos, $"{op} expected");
        }
    }

    private void Require(TokenType type)
    {
        if (_currentToken != null && !_currentToken.Equals(type))
        {
            throw new ParserException(_currentToken.Pos, $"{type} expected");
        }
    }

    private void Require(Keywords keyword)
    {
        if (_currentToken != null && !_currentToken.Equals(keyword))
        {
            throw new ParserException(_currentToken.Pos, $"{keyword} expected");
        }
    }

    private void Require(Keywords keyword1, Keywords keyword2)
    {
        if (_currentToken != null && !(_currentToken.Equals(keyword1) || _currentToken.Equals(keyword2)))
        {
            throw new ParserException(_currentToken.Pos, $"{keyword1} or {keyword2} expected");
        }
    }

    void CopyElements(List<SyntaxNode> a, List<SyntaxNode> b)
    {
        a.AddRange(b);
    }

    public List<SyntaxNode> Declarations(bool parseFunctions = false)
    {
        var declarations = new List<SyntaxNode>();
        while (true)
        {
            if (_currentToken.Equals(Keywords.Type))
            {
                _currentToken = _scanner.GetToken();
                CopyElements(declarations, TypeDeclarations());
            }
            else if (_currentToken.Equals(Keywords.Const))
            {
                _currentToken = _scanner.GetToken();
                CopyElements(declarations, ConstDeclarations());
            }
            else if (_currentToken.Equals(Keywords.Var))
            {
                _currentToken = _scanner.GetToken();
                CopyElements(declarations, VarDeclarations());
            }
            else if (parseFunctions && _currentToken.Equals(Keywords.Procedure))
            {
                _currentToken = _scanner.GetToken();
                declarations.Add(ProcedureDeclaration());
            }
            else if (parseFunctions && _currentToken.Equals(Keywords.Function))
            {
                _currentToken = _scanner.GetToken();
                declarations.Add(FunctionDeclaration());
            }
            else
            {
                break;
            }
        }

        return declarations;
    }

    private List<SyntaxNode> TypeDeclarations()
    {
        var declarations = new List<SyntaxNode> { TypeDeclaration() };
        while (_currentToken.Equals(TokenType.Id))
        {
            declarations.Add(TypeDeclaration());
        }

        return declarations;
    }

    private SyntaxNode TypeDeclaration()
    {
        var id = Id();
        Require(Operation.Eq);
        _currentToken = _scanner.GetToken();
        var type = Type();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        return new NodeTypeDeclaration(id, type);
    }

    private List<SyntaxNode> ConstDeclarations()
    {
        var declarations = new List<SyntaxNode>() { ConstDeclaration() };
        while (_currentToken.Equals(TokenType.Id))
        {
            declarations.Add(ConstDeclaration());
        }

        return declarations;
    }

    private SyntaxNode ConstDeclaration()
    {
        var id = Id();
        SyntaxNode? type = null;
        if (_currentToken.Equals(Separator.Col))
        {
            _currentToken = _scanner.GetToken();
            type = Type();
        }
        Require(Operation.Eq);
        _currentToken = _scanner.GetToken();
        var value = Expression();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        return new NodeConstantDeclaration(id, type, value);
    }

    private List<SyntaxNode> VarDeclarations()
    {
        var declarations = new List<SyntaxNode>();
        CopyElements(declarations, VarDeclaration());
        while (_currentToken.Equals(TokenType.Id))
        {
            CopyElements(declarations, VarDeclaration());
        }
        return declarations;
    }

    private List<SyntaxNode> VarDeclaration()
    {
        var ids = IdList();
        Require(Separator.Col);
        _currentToken = _scanner.GetToken();
        var type = Type();
        SyntaxNode? value = null;
        if (_currentToken.Equals(Operation.Eq))
        {
            _currentToken = _scanner.GetToken();
            value = Expression();
        }

        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        if (value != null && ids.Count > 1)
        {
            // var a: Integer; b : Integer; begin end.
            throw new ParserException(_currentToken.Pos, "few id with expression in var declaration");
        }

        return ids.Select(id => new NodeVariableDeclaration(id, type, value)).Cast<SyntaxNode>().ToList();
    }

    public SyntaxNode Program()
    {
        var declarations = Declarations(true);
        Require(Keywords.Begin);
        _currentToken = _scanner.GetToken();
        var begin = BeginStatement();
        Require(Separator.Dot);
        return new NodeProgram(declarations, begin);
    }

    public List<SyntaxNode> ParametersDeclaration()
    {
        if (_currentToken.Equals(Separator.RPar))
        {
            return new List<SyntaxNode>();
        }
        var declarations = new List<SyntaxNode>();
        CopyElements(declarations, ParameterDeclaration());
        while (_currentToken.Equals(Separator.Sem))
        {
            _currentToken = _scanner.GetToken();
            CopyElements(declarations, ParameterDeclaration());
            if (_currentToken.Equals(Separator.RPar))
            {
                break;
            }
        }

        return declarations;
    }

    public List<SyntaxNode> ParameterDeclaration()
    {
        SyntaxNode? mod = null;
        if (_currentToken.Equals(Keywords.Var) || _currentToken.Equals(Keywords.Const))
        {
            mod = new NodeVariable(_currentToken);
            _currentToken = _scanner.GetToken();
        }

        var ids = IdList();
        Require(Separator.Col);
        _currentToken = _scanner.GetToken();
        var type = Type();
        return ids.Select(id => new NodeParameterDeclaration(mod, id, type)).Cast<SyntaxNode>().ToList();
    }

    public SyntaxNode ProcedureDeclaration()
    {
        var id = Id();
        Require(Separator.LPar);
        _currentToken = _scanner.GetToken();
        var @params = ParametersDeclaration();
        Require(Separator.RPar);
        _currentToken = _scanner.GetToken();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        var declarations = Declarations();
        Require(Keywords.Begin);
        _currentToken = _scanner.GetToken();
        var body = BeginStatement();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        return new NodeProcedureDeclaration(id, @params, declarations, body);
    }

    public SyntaxNode FunctionDeclaration()
    {
        var id = Id();
        Require(Separator.LPar);
        _currentToken = _scanner.GetToken();
        var @params = ParametersDeclaration();
        Require(Separator.RPar);
        _currentToken = _scanner.GetToken();
        Require(Separator.Col);
        _currentToken = _scanner.GetToken();
        var type = Type();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        var declarations = Declarations();
        Require(Keywords.Begin);
        _currentToken = _scanner.GetToken();
        var body = BeginStatement();
        Require(Separator.Sem);
        _currentToken = _scanner.GetToken();
        return new NodeFunctionDeclaration(id, @params, declarations, body, type);
    }
}