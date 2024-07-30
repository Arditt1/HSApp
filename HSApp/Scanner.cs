using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

public class Scanner
{
    private Stopwatch _stopwatch;
    private readonly DBLite _dbLite;

    public Scanner(string databasePath)
    {
        _dbLite = new DBLite(databasePath);
    }

    public async Task ScanDirectoryAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Directory does not exist.");
            return;
        }

        _stopwatch = Stopwatch.StartNew();

        await ProcessDirectoryAsync(folderPath);

        _stopwatch.Stop();

        Console.WriteLine($"Scanning completed in {_stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }

    private async Task ProcessDirectoryAsync(string directoryPath)
    {
        //var dbHandler = new DBLite("C:\\Users\\ardit\\source\\repos\\HSApp\\HSApp\\DB_Cache\\HSLite.db");
        var filePaths = Directory.GetFiles(directoryPath);
        var subDirectories = Directory.GetDirectories(directoryPath);

        var fileCount = filePaths.Length;
        if (fileCount > 0)
        {
            Console.WriteLine($"Processing {fileCount} file(s) in '{directoryPath}'.");
        }

        var fileTasks = new List<Task>();
        foreach (var filePath in filePaths)
        {
            fileTasks.Add(Task.Run(() => ProcessFile(filePath)));
        }

        await Task.WhenAll(fileTasks);

        // subdirectories
        var subDirectoryTasks = new List<Task>();
        foreach (var subDirectory in subDirectories)
        {
            subDirectoryTasks.Add(ProcessDirectoryAsync(subDirectory));
        }

        await Task.WhenAll(subDirectoryTasks);
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            using var stream = fileInfo.OpenRead();
            var md5Hash = ComputeMd5Hash(stream);//305d357d8a1086e38dbb73d9b7d7172b
            var sha1Hash = ComputeSha1Hash(stream);//da39a3ee5e6b4b0d3255bfef95601890afd80709
            stream.Position = 0;
            var sha256Hash = ComputeSha256Hash(stream);//f32ca6cc3d2d7914edbb1e288f9ea52677b023a3546bbf0a58f74c90fc5a60ff


            if (_dbLite.IsFileCached(filePath))
            {
                _dbLite.Increment(sha256Hash);
                Console.WriteLine($"This {filePath} has been processed!");
                return;
            }
            _dbLite.InsertOrUpdateFile(sha256Hash, md5Hash, sha1Hash, fileInfo.Length);

            _dbLite.AddFileToCache(filePath);

            Console.WriteLine($"{filePath}: MD5={md5Hash}, SHA1={sha1Hash}, SHA256={sha256Hash}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
        }
    }

    public static string ComputeMd5Hash(Stream stream)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public static string ComputeSha1Hash(Stream stream)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public static string ComputeSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
