using Scanner;

if (args.Length == 0)
{
    Console.WriteLine("Need to set a path to file");
    return 1;
}

var filePath = args[1];

if (!File.Exists(filePath))
{
    Console.WriteLine("File doesnt exist");
    return 1;
}

var stream = new StreamReader(filePath);
var scanner = new Scanner.Scanner(stream);

for (;;)
{
    var token = scanner.GetToken();
    if (token.Type == TokenType.Eof) break;

    Console.WriteLine(token.ToString());
}

return 0;