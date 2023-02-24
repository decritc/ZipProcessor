using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipProcessor.Services
{
    public class ZipFileProcessingService
    {
        public Dictionary<string, List<string>> ExtractPurchaseOrderPdfs(string path)
        {
            var filesToUpload = new Dictionary<string, List<string>>();
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                var csv = archive.Entries.Where(x => x.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
                
                using (var csvReader = new CsvReader(new StreamReader(csv!.Open()), new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "~" }))
                {
                    var records = csvReader.GetRecords<dynamic>().ToList();
                    foreach (var record in records)
                    {
                        IDictionary<string, object> propertyValues = (IDictionary<string, object>)record;
                        var po = propertyValues["PO Number"] as string;
                        var attachmentList = propertyValues["Attachment List"] as string;
                        var filePaths = attachmentList!.Split(',');
                        foreach (var filePath in filePaths)
                        {
                            var fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
                            if (filesToUpload.ContainsKey(po!))
                            {
                                filesToUpload[po!].Add(fileName);
                            }
                            else
                            {
                                filesToUpload.Add(po!, new List<string>() { fileName });
                            }
                        }
                    }
                }
            }
            return filesToUpload;
        }

        public async Task UploadPdfFile(S3BucketService s3Service, string zipFilePath, string PONumber, string pdfName)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                var pdf = archive.Entries.Where(x => x.FullName == pdfName).SingleOrDefault();
                var path = $"by-po/{PONumber}/{pdfName}";
                if (pdf != null)
                {
                    var memoryStream = new MemoryStream();

                    pdf.Open().CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    await s3Service.SaveFile(path, memoryStream);

                }
            }
        }
    }
}
