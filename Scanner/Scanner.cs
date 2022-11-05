using System.Globalization;

namespace Scanner;

public class Scanner : BufferedReader
{
    public Scanner(StreamReader streamReader) : base(streamReader)
    {
    }

    public Token GetToken()
    {
        var c = SkipCommentsAndSpaces();

        BufferSet(c.ToString());

        if (Eof())
            return CreateToken(TokenType.Eof, "");

        if (IsDigit(c, 10))
            return ReadNumber();

        if (IsNotDecimalNumberBegin(c))
            return ReadNotDecimalNumber(c);

        if (IsIdBegin(c))
            return ReadId();

        if (IsStringBegin(c))
            return ReadString();

        var token = ReadOperationOrSeparator();

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
                throw new Exception();
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
            2 => IsBinDigit(c),
            _ => throw new ArgumentOutOfRangeException(nameof(@base), @base, null)
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
            if (digits == 0) throw new Exception();
        }
        
        if (GetIfEqual('e') || GetIfEqual('E'))
        {
            var _ = GetIfEqual('+') || GetIfEqual('-');
            digits = Digits(10);
            type = TokenType.LitDob;
            if (digits == 0) throw new Exception();
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
            throw new Exception();
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

        if (digits == 0) throw new Exception();

        try
        {
            return CreateToken(TokenType.LitInt, Convert.ToInt32(Buffer.Substring(1, Buffer.Length - 1), (int)@base));
        }
        catch (OverflowException)
        {
            throw new Exception();
        }
    }

    private static bool IsIdBegin(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    }

    private static bool IsIdContinuation(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' or >= '0' and <= '9';
    }

    private Token ReadId()
    {
        while (IsIdContinuation((char)Peek()))
        {
            Get();
        }

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
            Get();
        } while (BufferPeek() != '\'');

        return CreateToken(TokenType.LitStr, Buffer);
    }

    private Token CreateToken(TokenType type, object value)
    {
        return new Token(Pos, type, value, Buffer);
    }
}