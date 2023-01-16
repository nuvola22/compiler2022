namespace Scanner;

public class BufferedReader
{
    private readonly StreamReader _streamReader;
    private readonly Position _position;
    private string _buffer;
    private List<char> _char_buffer;

    protected string Buffer => _buffer;
    protected Position Pos => _position;

    protected BufferedReader(StreamReader streamReader)
    {
        _buffer = "";
        _position = new Position(1, 0);
        _streamReader = streamReader;
        _char_buffer = new List<char>();
    }

    protected int Get()
    {
        char c;
        if (_char_buffer.Count > 0)
        {
            c = _char_buffer[^1];
            _char_buffer.Remove(_char_buffer[^1]);
        }
        else
        {
            c = (char)_streamReader.Read();
        }
        // program hello;\n\r
        // begin end.
        if (c == '\n')
            _position.AddLine();
        else if (c != '\r') _position.AddColumn();
        _buffer += c;
        return c;
    }

    protected void UnGet()
    {
        _char_buffer.Add(_buffer[^1]);
        _buffer = _buffer.Remove(_buffer[^1]);
        _position.SubColumn();
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