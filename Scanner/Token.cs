﻿namespace Scanner;

public class Position : ICloneable
{
    private uint _line, _column;

    public Position()
    {
        _line = 1;
        _column = 0;
    }

    public Position(uint line, uint column)
    {
        _line = line;
        _column = column;
    }

    public void AddLine()
    {
        ++_line;
        _column = 0;
    }

    public void AddColumn()
    {
        ++_column;
    }

    public void SubColumn()
    {
        --_column;
    }
    
    public (uint, uint) GetValue()
    {
        return (_line, _column);
    }

    public override string ToString()
    {
        return $"({_line}, {_column})";
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public enum TokenType
{
    LitInt,
    LitDob,
    LitStr,
    Id,
    Keyword,
    Operation,
    Separator,
    Eof,
    Unexpected
}

public enum Operation
{
    Eq, // =
    Neq, // <>
    Lt, // <
    Gt, // >
    Lte, // <=
    Gte, // >=
    Add, // +
    Sub, // -
    Mul, // *
    Div, // /
    Bsl, // <<
    Bsr, // >>
    Asg, // :=
    AddAsg, // +=
    SubAsg, // -=
    MulAsg, // *=
    DivAsg // /=
}

public enum Separator
{
    LPar, // (
    RPar, // )
    LBra, // [
    RBra, // ]
    Comma, // ,
    Dot, // .
    Range, // ..
    Sem, // ;
    Col // :
}

public enum Keywords
{
    And,
    Array,
    Asm,
    Begin,
    Break,
    Case,
    Const,
    Constructor,
    Continue,
    Destructor,
    Div,
    Do,
    Downto,
    Else,
    End,
    False,
    File,
    For,
    Function,
    Goto,
    If,
    Implementation,
    In,
    Inline,
    Interface,
    Label,
    Mod,
    Nil,
    Not,
    Object,
    Of,
    Operator,
    Or,
    Packed,
    Procedure,
    Program,
    Record,
    Repeat,
    Set,
    Shl,
    Shr,
    String,
    Then,
    To,
    True,
    Type,
    Unit,
    Until,
    Uses,
    Var,
    While,
    With,
    Xor,
    As,
    Class,
    Constref,
    Dispose,
    Except,
    Exit,
    Exports,
    Finalization,
    Finally,
    Inherited,
    Initialization,
    Is,
    Library,
    New,
    On,
    Out,
    Property,
    Raise,
    Self,
    Threadvar,
    Try,
    Absolute,
    Abstract,
    Alias,
    Assembler,
    Cdecl,
    Cppdecl,
    Default,
    Export,
    External,
    Forward,
    Generic,
    Index,
    Local,
    Name,
    Nostackframe,
    Oldfpccall,
    Override,
    Pascal,
    Private,
    Protected,
    Public,
    Published,
    Read,
    Register,
    Reintroduce,
    Safecall,
    Softfloat,
    Specialize,
    Stdcall,
    Virtual,
    Write
}

public class Token : ICloneable
{
    private Position _pos;
    private TokenType _type;
    private object _value;
    private string _raw;

    public Position Pos => _pos;
    public TokenType Type => _type;
    public object Value => _value;
    public string Raw => _raw;

    public Token(Position pos, TokenType type, object value, string raw)
    {
        _pos = pos;
        _type = type;
        _value = value;
        _raw = raw;
    }

    public bool Equals(params Operation[] ops)
    {
        return ops.Any(op => Equals(TokenType.Operation) && ((Operation)_value).Equals(op));
    }

    public bool Equals(Separator sep)
    {
        return Equals(TokenType.Separator) && ((Separator)_value).Equals(sep);
    }

    public bool Equals(Keywords keyword)
    {
        return Equals(TokenType.Keyword) && ((Keywords)_value).Equals(keyword);
    }

    public bool Equals(TokenType type)
    {
        return Type == type;
    }

    public bool EqualsArray(params object[] args)
    {
        return args.Any(arg => Equals(Convert.ChangeType(arg, arg.GetType())));
    }

    public override string ToString()
    {
        var (line, column) = _pos.GetValue();
        return $"{line}\t\t{column}\t\t{Type.ToString()}\t\t\t{Value}\t\t\t\t{Raw}";
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}