namespace Scanner;

public class BufferedReader
{
    private readonly StreamReader _streamReader;
    private readonly Position _position;
    private string _buffer;

    protected string Buffer => _buffer;
    protected Position Pos => _position;

    protected BufferedReader(StreamReader streamReader)
    {
        _buffer = "";
        _position = new Position(1, 0);
        _streamReader = streamReader;
    }

    protected int Get()
    {
        var c = (char)Peek();
        if (c == '\n')
            _position.AddLine();
        else if (c != '\r') _position.AddColumn();
        _buffer += c;
        return _streamReader.Read();
    }

    protected int Peek()
    {
        return _streamReader.Peek();
    }

    protected int BufferPeek()
    {
        return _buffer[^1];
    }

    protected void BufferSet(string str)
    {
        _buffer = str;
    }

    protected bool GetIfEqual(char c)
    {
        if ((char)Peek() == c)
        {
            Get();
            return true;
        }

        return false;
    }

    // End of file
    protected bool Eof()
    {
        return _streamReader.EndOfStream;
    }
}