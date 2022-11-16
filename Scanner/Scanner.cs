using System.Globalization;

namespace Scanner;

public class ScannerException : Exception
{
    public ScannerException(Position pos, string message)
        : base($"{pos}, {message}")
    {
    }
}

public class Scanner : BufferedReader
{
    private Position _posInTokenBegin;

    public Scanner(StreamReader streamReader) : base(streamReader)
    {
        _posInTokenBegin = Pos;
    }

    public Token GetToken()
    {
        var c = SkipCommentsAndSpaces();
        _posInTokenBegin = (Position)Pos.Clone();
        BufferSet(c.ToString());

        if (IsDigit(c, 10))
            return ReadNumber();

        if (IsNotDecimalNumberBegin(c))
            return ReadNotDecimalNumber(c);

        if (IsIdBegin(c))
            return ReadId();

        if (IsStringBegin(c))
            return ReadString();

        var token = ReadOperationOrSeparator();

        if (Eof())
            return CreateToken(TokenType.Eof, "");

        return token;
    }

    /**
     * skip spaces, CR, LF, comments
     */
    private char SkipCommentsAndSpaces()
    {
        var c = (char)Get();
        for (;;)
            // spaces
            // on linux and mac, \n - next line
            // on win, \n\r - next line
            if (c is ' ' or '\n' or '\t' or '\r')
            {
                c = (char)Get();
            }
            else if (c == '/' && GetIfEqual('/'))
            {
                SkipLineComment();
                c = (char)Get();
            }
            else if (c == '{')
            {
                SkipBlockComment(true);
                c = (char)Get();
            }
            else if (c == '(' && GetIfEqual('*'))
            {
                SkipBlockComment(false);
                c = (char)Get();
            }
            else
            {
                break;
            }

        return c;
    }

    private void SkipLineComment()
    {
        while (Peek() != '\n')
        {
            Get();
            if (Eof())
                break;
        }
    }

    private void SkipBlockComment(bool firstBrace)
    {
        Get();
        while ((!firstBrace && !(BufferPeek() == '*' && GetIfEqual(')'))) ||
               (firstBrace && BufferPeek() != '}'))
        {
            Get();
            if (Eof())
                throw CreateException("Comment doesnt closed");
        }
    }

    private static bool IsDecDigit(char c)
    {
        return c is >= '0' and <= '9';
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private static bool IsOctDigit(char c)
    {
        return c is >= '0' and <= '7';
    }

    private static bool IsBinDigit(char c)
    {
        return c is >= '0' and <= '1';
    }

    private static bool IsDigit(char c, uint @base)
    {
        return @base switch
        {
            10 => IsDecDigit(c),
            16 => IsHexDigit(c),
            8 => IsOctDigit(c),
            _ => IsBinDigit(c)
        };
    }

    private uint Digits(uint @base)
    {
        uint count = 0;
        while (IsDigit((char)Peek(), @base))
        {
            ++count;
            Get();
        }

        return count;
    }

    private Token ReadNumber()
    {
        uint digits;
        var type = TokenType.LitInt;

        Digits(10);

        if (GetIfEqual('.'))
        {
            digits = Digits(10);
            type = TokenType.LitDob;
            if (digits == 0) throw CreateException("Invalid Integer");
        }

        if (GetIfEqual('e') || GetIfEqual('E'))
        {
            var _ = GetIfEqual('+') || GetIfEqual('-');
            digits = Digits(10);
            type = TokenType.LitDob;
            if (digits == 0) throw CreateException("Invalid Integer");
        }

        if (type == TokenType.LitDob)
            try
            {
                return CreateToken(type, double.Parse(Buffer, CultureInfo.InvariantCulture));
            }
            catch (OverflowException)
            {
                return CreateToken(type, double.PositiveInfinity);
            }

        try
        {
            return CreateToken(type, int.Parse(Buffer));
        }
        catch (OverflowException)
        {
            throw CreateException("Integer Overflow");
        }
    }

    private bool IsNotDecimalNumberBegin(char prefix)
    {
        return prefix is '$' or '&' or '%';
    }

    private Token ReadNotDecimalNumber(char c)
    {
        uint @base = c switch
        {
            '$' => 16,
            '&' => 8,
            '%' => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

        var digits = Digits(@base);

        if (digits == 0) throw CreateException("Invalid Integer");

        try
        {
            return CreateToken(TokenType.LitInt, Convert.ToInt32(Buffer.Substring(1, Buffer.Length - 1), (int)@base));
        }
        catch (OverflowException)
        {
            throw CreateException("Integer Overflow");
        }
    }

    private static bool IsIdBegin(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    }

    private static bool IsIdContinuation(char c)
    {
        return IsIdBegin(c) || IsDigit(c, 10);
    }

    private Token ReadId()
    {
        while (IsIdContinuation((char)Peek())) Get();

        // program
        // 
        try
        {
            return CreateToken(TokenType.Keyword,
                (Keywords)Enum.Parse(typeof(Keywords),
                    CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Buffer.ToLower()))
            );
        }
        catch (Exception)
        {
            return CreateToken(TokenType.Id, Buffer);
        }
    }

    private Token ReadOperationOrSeparator()
    {
        var punctuation = new List<(string, (TokenType, object))>
        {
            ("(", (TokenType.Separator, Separator.LPar)),
            (")", (TokenType.Separator, Separator.RPar)),
            ("[", (TokenType.Separator, Separator.LBra)),
            ("]", (TokenType.Separator, Separator.RBra)),
            (".", (TokenType.Separator, Separator.Dot)),
            ("..", (TokenType.Separator, Separator.Range)),
            (";", (TokenType.Separator, Separator.Sem)),
            (":", (TokenType.Separator, Separator.Col)),

            ("=", (TokenType.Operation, Operation.Eq)),
            ("<>", (TokenType.Operation, Operation.Neq)),
            ("<", (TokenType.Operation, Operation.Lt)),
            (">", (TokenType.Operation, Operation.Gt)),
            ("<=", (TokenType.Operation, Operation.Lte)),
            (">=", (TokenType.Operation, Operation.Gte)),
            ("+", (TokenType.Operation, Operation.Add)),
            ("-", (TokenType.Operation, Operation.Sub)),
            ("*", (TokenType.Operation, Operation.Mul)),
            ("/", (TokenType.Operation, Operation.Div)),
            ("<<", (TokenType.Operation, Operation.Bsl)),
            (">>", (TokenType.Operation, Operation.Bsr)),
            (":=", (TokenType.Operation, Operation.Asg)),
            ("+=", (TokenType.Operation, Operation.AddAsg)),
            ("-=", (TokenType.Operation, Operation.SubAsg)),
            ("*=", (TokenType.Operation, Operation.MulAsg)),
            ("/=", (TokenType.Operation, Operation.DivAsg))
        };

        var token = CreateToken(TokenType.Unexpected, "");

        var applicant = punctuation.Find(item => item.Item1 == Buffer);

        if (applicant != default) token = CreateToken(applicant.Item2.Item1, applicant.Item2.Item2);

        applicant = punctuation.Find(item => item.Item1 == Buffer + (char)Peek());

        if (applicant != default)
        {
            Get();
            token = CreateToken(applicant.Item2.Item1, applicant.Item2.Item2);
        }

        return token;
    }

    private bool IsStringBegin(char c)
    {
        return c == '\'';
    }

    private Token ReadString()
    {
        do
        {
            var c = (char)Get();
            if (c == '\n') throw CreateException("String exceed line");
        } while (BufferPeek() != '\'');

        // 'abc'#65#66'123'
        return CreateToken(TokenType.LitStr, Buffer);
    }

    private Token CreateToken(TokenType type, object value)
    {
        return new Token(_posInTokenBegin, type, value, Buffer);
    }

    private ScannerException CreateException(string message)
    {
        return new ScannerException(_posInTokenBegin, message);
    }
}