using IonKiwi.lz4;
using System.Reflection;
using System.Text;

namespace lz4cmd;

public class Program
{
    static string ExeName { get; }
    static string ExeVersion { get; }
    static Program()
    {
        var asm = Assembly.GetEntryAssembly()?.GetName();
        ExeName = asm?.Name ?? "?";
        ExeVersion = asm?.Version?.ToString() ?? "?";
    }

    public static void Main(string[] args)
    {
        try
        {
            RunMain(args);
        }
        catch (Exception ee)
        {
            Console.WriteLine(ee.Message);
        }
    }

    static bool PrintSyntax()
    {
        Console.WriteLine($"{ExeName} v{ExeVersion} Yung, Chun Kau; yung.chun.kau@gmail.com");
        Console.WriteLine($"""
            Compress:
              lz4cmd INFILE OUTFILE [OPTION]
            Decompress:
              lz4cmd -d INFILE OUTFILE [OPTION]
            INFILE and OUTFILE can be - for standard in and standard out.
            If INFILE or OUTFILE is skipped, - would be taken.
            OPTION:
              -b  NUMBER-OF-1K-BLOCK
            The default value of NUMBER-OF-1K-BLOCK is 4096.
            """);
        return false;
    }

    static bool PrintTooSimple()
    {
        Console.WriteLine($"Run '{ExeName} -?' for help");
        return false;
    }

    const string shortConsole = "-";

    static int CountOfBlock = 4096;
    static IEnumerable<string> GetNumberOf1kBlock(
        IEnumerable<string> args)
    {
        var it = args.GetEnumerator();
        while (it.MoveNext())
        {
            var current = it.Current;
            if ("-b" == current)
            {
                if (it.MoveNext())
                {
                    current = it.Current;
                    if (int.TryParse(current, out int cntBlock))
                    {
                        if (cntBlock > 0)
                        {
                            CountOfBlock = cntBlock;
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"Value to '-b' should be positive number, but {current} is found.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Value '{current}' is bad to '-b'");
                    }
                }
                else
                {
                    throw new ArgumentException(
                        "Value is required to '-b'");
                }
            }
            else
            {
                yield return current;
            }
        }
    }

    static bool RunMain(string[] mainArgs)
    {
        if (mainArgs.Contains("-?") ||
            mainArgs.Contains("-h") ||
            mainArgs.Contains("--help"))
        {
            return PrintSyntax();
        }

        if (mainArgs.Contains("--version"))
        {
            return PrintExeVersion();
        }

        var aa = GetNumberOf1kBlock(mainArgs.AsEnumerable()).GroupBy(
            (it) => it.Equals("-d") || it.Equals("--decompress"))
            .ToDictionary((it) => it.Key, (it) => it);

        string[] args;
        if (aa.ContainsKey(false))
        {
            args = aa[false].ToArray();
            if (args.Length == 1)
            {
                args = new string[2] { args[0], shortConsole };
            }
        }
        else
        {
            args = new string[2] { shortConsole, shortConsole};
        }

        if (args.Length != 2)
        {
            return PrintTooSimple();
        }

        if ((args[0] == shortConsole) && !Console.IsInputRedirected)
        {
            return PrintTooSimple();
        }

        Func<Stream, Action<Stream>, Stream, Action<Stream>, bool> funcThe = Compress;
        if (aa.ContainsKey(true))
        {
            funcThe = Decompress;
        }

        Action<Stream> CommonCloseFile = (fs) => fs.Close();
        Action<Stream> NoClose = (_) => { };

        if (args[1] != shortConsole && File.Exists(args[1]))
        {
            Console.WriteLine($"Output file '{args[1]}' ALREADY exists!");
            return false;
        }

        if (args[0] == shortConsole)
        {
            Console.InputEncoding = Encoding.GetEncoding("ISO-8859-1");
        }

        if (args[1] == shortConsole)
        {
            Console.OutputEncoding = Encoding.GetEncoding("ISO-8859-1");
        }

        return (args[0], args[1]) switch
        {
            (shortConsole, shortConsole) => funcThe(GetConsoleInput(), NoClose,
                GetConsoleOutput(), NoClose),

            (shortConsole, string) => funcThe(GetConsoleInput(), NoClose,
                File.Create(args[1]), CommonCloseFile),

            (string, shortConsole) => funcThe(File.OpenRead(args[0]), CommonCloseFile,
                GetConsoleOutput(), NoClose),

            _ => funcThe( File.OpenRead(args[0]), CommonCloseFile,
                File.Create(args[1]), CommonCloseFile)
        };
    }

    static bool Compress(Stream inpFile, Action<Stream> inputClose,
        Stream outFile, Action<Stream> outputClose)
    {
        Console.Error.WriteLine($"Number of 1-Kb block is {CountOfBlock}");
        var SIZE = CountOfBlock * 1024;
        var buffer1 = new byte[SIZE];
        var buffer2 = new byte[SIZE];
        bool isBuffer1 = true;
        using (LZ4Stream lz4file = LZ4Stream.CreateCompressor(
            outFile, LZ4StreamMode.Write, LZ4FrameBlockMode.Linked,
            LZ4FrameBlockSize.Max4MB, LZ4FrameChecksumMode.Content))
        {
            Task taskWrite = Stream.Null.WriteAsync(buffer2, 0, 0);
            Task<int> taskRead = inpFile.ReadAsync(buffer1, 0, SIZE);
            while (true)
            {
                taskWrite.Wait();
                taskRead.Wait();
                int readSize = taskRead.Result;
                if (readSize < 1) break;
                if (isBuffer1)
                {
                    isBuffer1 = false;
                    taskWrite = lz4file.WriteAsync(buffer1, 0, readSize);
                    taskRead = inpFile.ReadAsync(buffer2, 0, SIZE);
                }
                else
                {
                    isBuffer1 = true;
                    taskWrite = lz4file.WriteAsync(buffer2, 0, readSize);
                    taskRead = inpFile.ReadAsync(buffer1, 0, SIZE);
                }
            }
        }
        outputClose(outFile);
        inputClose(inpFile);
        return true;
    }

    static bool Decompress(Stream inpFile, Action<Stream> inputClose,
        Stream outFile, Action<Stream> outputClose)
    {
        Console.Error.WriteLine($"Number of 1-Kb block is {CountOfBlock}");
        var SIZE = CountOfBlock * 1024;
        var buffer1 = new byte[SIZE];
        var buffer2 = new byte[SIZE];
        bool isBuffer1 = true;

        using (LZ4Stream lz4Stream = LZ4Stream.CreateDecompressor(inpFile, 
            LZ4StreamMode.Read))
        {
            Task taskWrite = Stream.Null.WriteAsync(buffer2, 0, 0);
            Task<int> taskRead = lz4Stream.ReadAsync(buffer1, 0, SIZE);
            while (true)
            {
                taskWrite.Wait();
                taskRead.Wait();
                int readSize = taskRead.Result;
                if (readSize < 1) break;
                if (isBuffer1)
                {
                    isBuffer1 = false;
                    taskWrite = outFile.WriteAsync(buffer1, 0, readSize);
                    taskRead = lz4Stream.ReadAsync(buffer2, 0, SIZE);
                }
                else
                {
                    isBuffer1 = true;
                    taskWrite = outFile.WriteAsync(buffer2, 0, readSize);
                    taskRead = lz4Stream.ReadAsync(buffer1, 0, SIZE);
                }
            }
        }
        outputClose(outFile);
        inputClose(inpFile);
        return true;
    }

    static readonly Encoding Encoding8859 = Encoding.GetEncoding("ISO-8859-1");

    static Stream GetConsoleInput()
    {
        Console.InputEncoding = Encoding8859;
        return Console.OpenStandardInput();
    }

    static Stream GetConsoleOutput()
    {
        Console.OutputEncoding = Encoding8859;
        return Console.OpenStandardOutput();
    }

    static bool PrintExeVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var asmName = asm.GetName();
        var ExeName = asmName.Name ?? "?";
        var ExeVersion = asmName.Version?.ToString() ?? "?";
        var ExeCopyright = "?";
        var aa = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute),
            inherit: false);
        if (aa.Length > 0)
        {
            ExeCopyright = ((AssemblyCopyrightAttribute)aa[0]).Copyright;
        }
        Console.WriteLine($"{ExeName} v{ExeVersion} {ExeCopyright}");
        return false;
    }
}
