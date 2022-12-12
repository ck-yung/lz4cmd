using IonKiwi.lz4;
using System;
using System.IO.Compression;

namespace lz4cmd;

public class Program
{
    const string version = "1.0.0.2";
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
        Console.WriteLine($"lz4cmd v{version} Yung, Chun Kau; yung.chun.kau@gmail.com");
        Console.WriteLine($"""
            Compress:
              lz4cmd INFILE OUTFILE
            Decompress:
              lz4cmd -d INFILE OUTFILE
            INFILE and OUTFILE can be - for standard in and standard out.
            If INFILE or OUTFILE is skipped, - would be taken.
            """);
        return false;
    }

    static bool PrintTooSimple()
    {
        Console.WriteLine("Run 'lz4cmd -?' for help");
        return false;
    }

    const string shortConsole = "-";

    static bool RunMain(string[] mainArgs)
    {
        if (mainArgs.Contains("-?") || mainArgs.Contains("-h"))
        {
            return PrintSyntax();
        }

        var aa = mainArgs.GroupBy((it) => it.Equals("-d"))
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

        return (args[0], args[1]) switch
        {
            (shortConsole, shortConsole) => funcThe(Console.OpenStandardInput(), NoClose, 
                Console.OpenStandardOutput(), NoClose),

            (shortConsole, string) => funcThe(Console.OpenStandardInput(), NoClose,
                File.Create(args[1]), CommonCloseFile),

            (string, shortConsole) => funcThe(File.OpenRead(args[0]), CommonCloseFile,
                Console.OpenStandardOutput(), NoClose),

            _ => funcThe( File.OpenRead(args[0]), CommonCloseFile,
                File.Create(args[1]), CommonCloseFile)
        };
    }

    static bool Compress(Stream inpFile, Action<Stream> inputClose,
        Stream outFile, Action<Stream> outputClose)
    {
        const int SIZE = 4 * 1024 * 1024;
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
        const int SIZE = 4 * 1024 * 1024;
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
}
