using Microsoft.Extensions.Configuration;
using ZipProcessor.Models;
using ZipProcessor.Services;

public class Program
{
    private static async Task Main(string[] args)
    {

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        var configuration = builder.Build();

        S3BucketService s3BucketService = new S3BucketService(configuration);

        var metaData = await s3BucketService.GetMetaData();
        var fileNames = await s3BucketService.GetZipFileNames();
        var filesToProcess = fileNames.Where(
            x => !metaData.ProcessedZipFiles.Where(p => p.Name == x).Any()
            ).ToList();
        ZipFileProcessingService zipFileProcessingService = new ZipFileProcessingService();
        foreach (var fileName in filesToProcess)
        {
            ProcessedZipFile processedZipFile = await ProcessFile(s3BucketService, zipFileProcessingService, fileName);
            metaData.ProcessedZipFiles.Add(processedZipFile);
        }

        await s3BucketService.SaveMetaData(metaData);

        CleanupTempFiles();
    }

    private static void CleanupTempFiles()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
        directoryInfo.GetDirectories().Where(x => x.Name == "temp").Single().Delete(true);
    }

    private static async Task<ProcessedZipFile> ProcessFile(S3BucketService s3BucketService, ZipFileProcessingService zipFileProcessingService, string? fileName)
    {
        var processedZipFile = new ProcessedZipFile(fileName);
        var path = await s3BucketService.GetZipFile(fileName);
        var pdfDictionary = zipFileProcessingService.ExtractPurchaseOrderPdfs(path);
        foreach (var kvp in pdfDictionary)
        {
            foreach(string value in kvp.Value)
            {
                await zipFileProcessingService.UploadPdfFile(s3BucketService, path, kvp.Key, value);
                var pdfFile = new PdfFile(value);
                processedZipFile.PdfFiles.Add(pdfFile);
            }
        }
        processedZipFile.DateProcessed = DateTimeOffset.Now;
        return processedZipFile;
    }
}
