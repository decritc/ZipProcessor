using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using ZipProcessor.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Immutable;

namespace ZipProcessor.Services
{
    public class S3BucketService
    {
        AmazonS3Client _client;
        readonly string _bucketName;
        readonly string _basePath;
        readonly string _metaFileName;
        public S3BucketService(IConfiguration configuration)
        {
            _basePath = Directory.GetCurrentDirectory();
            _metaFileName = configuration["MetaFileName"] ?? throw new ArgumentNullException(nameof(configuration));
            _bucketName = configuration["S3:BucketName"] ?? throw new ArgumentNullException(nameof(configuration));

            _client = new AmazonS3Client(
                configuration["S3:AccessKey"],
                configuration["S3:Secret"],
                RegionEndpoint.GetBySystemName(configuration["S3:Region"])
            );
        }

        public async Task<ProcessingMetaData> GetMetaData()
        {
            var objectRequest = new ListObjectsRequest
            { 
                BucketName = _bucketName
            };
            var files = await _client.ListObjectsAsync(objectRequest);

            ProcessingMetaData? data = new ProcessingMetaData();
            var exists = files.S3Objects.Where(x => x.Key == _metaFileName).Any();
            if (exists)
            {
                var response = await _client.GetObjectAsync(_bucketName, _metaFileName);
                data = await JsonSerializer.DeserializeAsync<ProcessingMetaData>(response.ResponseStream);
            }
            else
            {
                await SaveMetaData(data);
            }

            return data;
        }

        public async Task<IReadOnlyList<string>> GetZipFileNames()
        {
            var objectRequest = new ListObjectsRequest
            {
                BucketName = _bucketName
            };
            var files = await _client.ListObjectsAsync(objectRequest);
            
            return files.S3Objects.Where(x => x.Key.Contains(".zip")).Select(x => x.Key).ToImmutableList();
        }
        
        public async Task<string> GetZipFile(string fileName)
        {
            var path = _basePath + @"\temp\" + fileName;
            TransferUtility transferUtility = new TransferUtility(_client);
            var downloadRequest = new TransferUtilityDownloadRequest
            {
                BucketName = _bucketName,
                FilePath = path,
                Key = fileName
            };

            await transferUtility.DownloadAsync(downloadRequest);
            return path;
        }
        public async Task SaveFile(string path, MemoryStream stream)
        {
            TransferUtility transferUtility = new TransferUtility(_client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                Key = path,
                InputStream = stream,
            };

            await transferUtility.UploadAsync(uploadRequest);
        }

        public async Task<PutObjectResponse> SaveMetaData(ProcessingMetaData data)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var jsondata = JsonSerializer.Serialize(data, options);
            var byteArray = Encoding.ASCII.GetBytes(jsondata);
            var stream = new MemoryStream(byteArray);
            
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = _metaFileName,
                InputStream = stream
            };

            return await _client.PutObjectAsync(putObjectRequest);
        }
    }
}
