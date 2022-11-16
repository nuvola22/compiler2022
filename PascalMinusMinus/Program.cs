using Scanner;

bool ArgExist(string arg)
{
    return args.Skip(1).Any(x => x == arg);
}

void RunScanner(string filePath)
{
    var stream = new StreamReader(filePath);
    var scanner = new Scanner.Scanner(stream);

    for (;;)
    {
        var token = scanner.GetToken();
        if (token.Type == TokenType.Eof) break;

        Console.WriteLine(token.ToString());
    }

    stream.Close();
}

void RunParser(string filePath)
{
    var stream = new StreamReader(filePath);
    var scanner = new Scanner.Scanner(stream);
    var parser = new Parser.Parser(scanner);
    var head = parser.ParseExpression();
    head.Draw(Console.Out, 0);
}

if (args.Length == 0)
{
    Console.WriteLine("Need to set a path to file");
    return 1;
}

var filePath = args[0];
Console.WriteLine(filePath);

if (!File.Exists(filePath))
{
    Console.WriteLine("File doesnt exist");
    return 1;
}

if (ArgExist("-s")) RunScanner(filePath);

if (ArgExist("-p")) RunParser(filePath);

return 0;