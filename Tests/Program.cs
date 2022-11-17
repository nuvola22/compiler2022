using System.Text.RegularExpressions;
using Scanner;

static IEnumerable<string> GetFiles(string dirPath)
{
    var reg = new Regex(@".in$");
    var files = Directory.GetFiles(dirPath, "*.in").Where(path => reg.IsMatch(path));
    files = files.Select(item => item[..^3]); // take without 3 last chars
   return files;
}

Result RunScannerTests(IEnumerable<string> files)
{
    var result = new Result();
    foreach (var file in files)
    {
        Scanner.Scanner? scanner;
        Token token;
        Console.WriteLine(file);

        if (!File.Exists(file + ".out"))
        {
            Console.WriteLine("doesnt exist");
            // write if file doesnt exist
            scanner = new Scanner.Scanner(new StreamReader(file + ".in"));

            var streamOutWriter = new StreamWriter(file + ".out");
            for (;;)
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

            streamOutWriter.Flush();
            streamOutWriter.Close();
            continue;
        }

        var streamInReader = new StreamReader(file + ".in");
        var streamOutReader = new StreamReader(file + ".out");
        scanner = new Scanner.Scanner(streamInReader);

        for (;;)
        {
            var line = streamOutReader.ReadLine();
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
                    result.MarkFailed();
                }
                else
                {
                    result.MarkSucceed();
                }

                break;
            }

            if (line != token.ToString())
            {
                Console.WriteLine("Failed Test");
                Console.WriteLine("Out File: {0}", line);
                Console.WriteLine("Scanner:  {0}", token);
                result.MarkFailed();
                break;
            }

            if (token.Type == TokenType.Eof)
            {
                result.MarkSucceed();
                break;
            }
        }
    }

    return result;
}

Result RunParserTests(IEnumerable<string> files)
{
    var result = new Result();
    
    foreach (var file in files)
    {
        var scanner = new Scanner.Scanner(new StreamReader(file + ".in"));
        var parser = new Parser.Parser(scanner);
        StringWriter writer;

        Console.WriteLine(file);

        if (!File.Exists(file + ".out"))
        {
            Console.WriteLine("doesnt exist");

            var streamOutWriter = new StreamWriter(file + ".out");
            try
            {
                writer = new StringWriter();
                parser.ParseExpression().Draw(writer, 1);
                streamOutWriter.WriteLine(writer.ToString());
                Console.WriteLine(writer.ToString());
            }
            catch (Exception ex)
            {
                streamOutWriter.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
            }
            streamOutWriter.Flush();
            streamOutWriter.Close();
            continue;
        }
        
        var outContent = File.ReadAllText(file + ".out");

        writer = new StringWriter();
        parser.ParseExpression().Draw(writer, 1);
        var parserResult = writer.ToString();

        if (String.CompareOrdinal(outContent, parserResult) != 0)
        {
            result.MarkSucceed();
        }
        else
        {
            Console.WriteLine("FAILED");
            Console.WriteLine("Out file:");
            Console.WriteLine(outContent);
            Console.WriteLine("Parser:");
            Console.WriteLine(parserResult);
            result.MarkFailed();
        }
            
    }

    return result;
}

var res = RunScannerTests(GetFiles("../../../lexer/"));
res += RunParserTests(GetFiles("../../../parser"));

Console.WriteLine("Tests: {0}", res);

public class Result
{
    private uint _allTests;
    private uint _failedTests;

    public Result()
    {
        _allTests = 0;
        _failedTests = 0;
    }

    public Result(uint allTests, uint failedTests)
    {
        _allTests = allTests;
        _failedTests = failedTests;
    }

    public void MarkFailed()
    {
        _allTests++;
        _failedTests++;
    }

    public void MarkSucceed()
    {
        _allTests++;
    }

    public override string ToString()
    {
        return $"Success: {_allTests}, Failed: {_failedTests}";
    }

    public static Result operator +(Result a, Result b)
    {
        return new Result(a._allTests + b._allTests, a._failedTests + b._failedTests);
    }
};