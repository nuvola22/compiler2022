using System.Text.RegularExpressions;
using Scanner;

static IEnumerable<string> GetFiles(string dirPath)
{
    var reg = new Regex(@".in$");
    var files = Directory.GetFiles(dirPath, "*.in").Where(path => reg.IsMatch(path));
    files = files.Select(item => item[..^3]); // take without 3 last chars
    return files;
}

var files = GetFiles("../../../lexer/");

int countOfTests = 0,
    countOfFailedTests = 0;
foreach (var file in files)
{
    Scanner.Scanner? scanner;
    Token token = null;
    Console.WriteLine(file);
    
    if (!File.Exists(file + ".out"))
    {
        Console.WriteLine("doesnt exist");
        // write if file doesnt exist
        scanner = new Scanner.Scanner(new StreamReader(file + ".in"));

        var streamOutWriter = new StreamWriter(file + ".out");
        for (;;)
        {
            try
            {

                token = scanner.GetToken();
                streamOutWriter.WriteLine(token);
                if (token.Type == TokenType.Eof) break;
            }
            catch (Exception ex)
            {
                streamOutWriter.WriteLine(ex.Message);
                break;
            }
        }

        streamOutWriter.Flush();
        streamOutWriter.Close();
        continue;
    }

    var streamInReader = new StreamReader(file + ".in");
    var streamOutReader = new StreamReader(file + ".out");
    scanner = new Scanner.Scanner(streamInReader);
    string? line;

    for (;;)
    {
        line = streamOutReader.ReadLine();
        try
        {
            token = scanner.GetToken();
        }
        catch (Exception ex)
        {
            if (ex.Message != line)
            {
                Console.WriteLine("Failed Test");
                Console.WriteLine("Out File:   {0}", line);
                Console.WriteLine("Exception:  {0}", ex.Message);
                ++countOfFailedTests;
            }
            break;
        }

        if (line != token.ToString())
        {
            Console.WriteLine("Failed Test");
            Console.WriteLine("Out File: {0}", line);
            Console.WriteLine("Scanner:  {0}", token);
            ++countOfFailedTests;
            break;
        }

        if (token.Type == TokenType.Eof) break;
    }

    ++countOfTests;
}

Console.WriteLine("Tests: {0}, Failed: {1}", countOfTests, countOfFailedTests);