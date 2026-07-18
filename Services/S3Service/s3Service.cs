using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Services.S3Service;

public class s3Service : IS3Service
{
   private readonly IAmazonS3 _s3Client;
    private readonly ILogger<s3Service> _logger;
    private readonly string _bucketName;

    public s3Service(IAmazonS3 s3Client, ILogger<s3Service> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = Environment.GetEnvironmentVariable("UPLOAD_BUCKET")
            ?? Environment.GetEnvironmentVariable("BUCKET_NAME")
            ?? throw new InvalidOperationException("Bucket name not configured. Set UPLOAD_BUCKET or BUCKET_NAME.");
    }

    public async Task<Stream> DownloadFileFromS3Async(string s3Key)
    {
        _logger.LogInformation("Downloading file from S3. Bucket: {Bucket}, Key: {S3Key}", _bucketName, s3Key);

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        using var response = await _s3Client.GetObjectAsync(request);

        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        _logger.LogInformation(
            "Downloaded {Size} bytes for key {S3Key}",
            memoryStream.Length, s3Key);

        return memoryStream;
    }
}