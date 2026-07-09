using Amazon.Lambda.Core;
using Core.DTOs;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PresightLambda;

public class Function
{
    public FileKeyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var input = JsonSerializer.Deserialize<PresignRequest>(request.Body);

        context.Logger.LogInformation($"Input received: {JsonSerializer.Serialize(input)}");
        context.Logger.LogInformation( $"FileName received: {input?.FileName}");
        
        if (input is null || string.IsNullOrWhiteSpace(input.FileName))
        {
            context.Logger.LogInformation("PresightLambda invoked without fileName.");
            return new FileKeyResponse(string.Empty, string.Empty, "No File Upload Requested");
        }

        try
        {
            var fileKey = BuildFileKey(input.FileName);
            context.Logger.LogInformation($"Generated fileKey: {fileKey}");

            var bucketName = GetBucketName();
            var presignedUrl = GeneratePreSignedUrl(bucketName, fileKey);

            return new FileKeyResponse(fileKey, presignedUrl, "OK");
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogError($"Configuration error: {ex.Message}");
            return new FileKeyResponse(string.Empty, string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error generating fileKey: {ex}");
            return new FileKeyResponse(string.Empty, string.Empty, $"error: {ex.Message}");
        }
    }

    private static string BuildFileKey(string fileName)
    {
        var uniqueId = Guid.NewGuid().ToString();
        return $"sakshi/{uniqueId}__{fileName.Trim()}";
    }

    private static string GetBucketName()
    {
        var bucketName = Environment.GetEnvironmentVariable("UPLOAD_BUCKET") ?? Environment.GetEnvironmentVariable("BUCKET_NAME");
        Console.WriteLine($"BUCKET = {Environment.GetEnvironmentVariable("BUCKET_NAME")}");
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("bucket configuration missing");
        }

        return bucketName;
    }

    private static string GeneratePreSignedUrl(string bucketName, string fileKey)
    {
        var region = GetS3Region();
        var s3Config = new AmazonS3Config { RegionEndpoint = region };
        using var s3Client = new AmazonS3Client(s3Config);

        var presignRequest = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = fileKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(6000),
            ContentType = "application/pdf"
        };

        return s3Client.GetPreSignedURL(presignRequest);
    }

    private static RegionEndpoint GetS3Region()
    {
        var regionName = Environment.GetEnvironmentVariable("S3_REGION") ?? Environment.GetEnvironmentVariable("AWS_REGION");
        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException("S3 region is not configured. Set S3_REGION or AWS_REGION.");
        }

        return RegionEndpoint.GetBySystemName(regionName);
    }
}
