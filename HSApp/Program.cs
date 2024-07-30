using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        SQLitePCL.Batteries.Init();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("HSLog.txt")
            .CreateLogger();

     
        string path;
        do
        {
            Console.WriteLine("Please enter the folder or file path:");
            path = Console.ReadLine()?.Trim();
        } while (string.IsNullOrEmpty(path));

        //string databasePath = "C:\\Users\\ardit\\source\\repos\\HSApp\\HSApp\\DB_Cache\\HSLite.db";

        string currentDirectory = AppContext.BaseDirectory;
        string databasePath = Path.Combine(currentDirectory, "DB_Cache", "HSLite.db");
        if (!File.Exists(databasePath))
        {
            Console.WriteLine($"Database file not found at: {databasePath}");
            return;
        }

        var dbLite = new DBLite(databasePath);
        var fileScanner = new Scanner(databasePath);

        try
        {
            if (Directory.Exists(path))
            {
                Console.WriteLine($"Processing directory: {path}");
                await fileScanner.ScanDirectoryAsync(path);
            }
            else
            {
                Console.WriteLine("The specified path does not exist.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during processing.");
        }

        Log.CloseAndFlush();
    }
}
