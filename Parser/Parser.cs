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
    private Token _currentToken;

    public Parser(Scanner.Scanner scanner)
    {
        _scanner = scanner;
        _currentToken = scanner.GetToken();
    }

    public SyntaxNode ParseExpression()
    {
        var left = ParseTerm();
        var token = _currentToken;

        while (token.Type == TokenType.Operation &&
               ((Operation)token.Value == Operation.Add || (Operation)token.Value == Operation.Sub))
        {
            _currentToken = _scanner.GetToken();
            left = new NodeBinaryOp(token, left, ParseTerm());
            token = _currentToken;
        }

        return left;
    }

    private SyntaxNode ParseTerm()
    {
        var left = ParseFactor();
        var token = _currentToken;
        while (token.Type == TokenType.Operation &&
               ((Operation)token.Value == Operation.Mul || (Operation)token.Value == Operation.Div))
        {
            _currentToken = _scanner.GetToken();
            left = new NodeBinaryOp(token, left, ParseFactor());
            token = _currentToken;
        }

        return left;
    }

    private SyntaxNode ParseFactor()
    {
        var token = _currentToken;

        if (token.Type is TokenType.LitDob or TokenType.LitInt)
        {
            _currentToken = _scanner.GetToken();
            return new NodeNumber(token);
        }

        if (token.Type is TokenType.Id)
        {
            _currentToken = _scanner.GetToken();
            return new NodeVariable(token);
        }

        if (token.Type == TokenType.Separator && (Separator)token.Value == Separator.LPar)
        {
            _currentToken = _scanner.GetToken();
            var expression = ParseExpression();

            if (_currentToken.Type != TokenType.Separator ||
                !(_currentToken.Type == TokenType.Separator && (Separator)_currentToken.Value == Separator.RBra))
                throw new ParserException(_currentToken.Pos, "Expected )");

            _currentToken = _scanner.GetToken();
            return expression;
        }

        throw new ParserException(token.Pos, "Factor expected");
    }
}