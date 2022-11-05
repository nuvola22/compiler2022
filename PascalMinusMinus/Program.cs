using Scanner;

var stream = new StreamReader("C:/PascalMinusMinus/pascal.txt");

var scanner = new Scanner.Scanner(stream);

for (;;)
{
    var token = scanner.GetToken();
    if (token.Type == TokenType.Eof) break;

    Console.WriteLine(token.ToString());
}