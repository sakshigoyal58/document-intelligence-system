using Amazon.Lambda.Core;
using Core.Constants;
using Core.DTOs;
using Core.Helpers;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Services.DependencyInjection;
using Services.DynamoDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PresightLambda;
public class Function
{

    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly IDynamoDbService _dynamoDBService;
    private readonly ILogger<Function> _logger;
    private readonly IConfiguration _configuration;

    public Function()
    {
        _dynamoDBService =
            _provider.GetRequiredService<IDynamoDbService>();

        _logger =
            _provider.GetRequiredService<ILogger<Function>>();

        _configuration =
            _provider.GetRequiredService<IConfiguration>();
    }
    public async Task<FileKeyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        PresignRequest? input;

        try
        {
            input = ParseRequest(request);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "PresightLambda received invalid request payload.");
            return new FileKeyResponse(string.Empty, string.Empty, "Invalid request payload.");
        }

        _logger.LogInformation("PresightLambda received fileName: {FileName}", input?.FileName);

        if (input is null || string.IsNullOrWhiteSpace(input.FileName))
        {
            _logger.LogInformation("PresightLambda invoked without fileName.");
            return new FileKeyResponse(string.Empty, string.Empty, "No File Upload Requested");
        }

        try
        {
            var documentId = Guid.NewGuid().ToString();
            var fileKey = DocumentStorageKey.Build(documentId, input.FileName);
            context.Logger.LogInformation("Generated fileKey: {FileKey}", fileKey);

            var bucketName = GetBucketName();
            var presignedUrl = GeneratePreSignedUrl(bucketName, fileKey);

            await _dynamoDBService.AddFileRecordAsync(documentId, input.FileName, 0, fileKey);
            await _dynamoDBService.UpdateFileStatusAsync(new UpdateStatusRequest
            {
                DocumentId = documentId,
                Status = DocumentStatus.Uploading,
                FileName = input.FileName,
                S3Key = fileKey
            });

            return new FileKeyResponse(fileKey, presignedUrl, "OK");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuration error while generating presigned URL.");
            return new FileKeyResponse(string.Empty, string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating fileKey.");
            return new FileKeyResponse(string.Empty, string.Empty, $"error: {ex.Message}");
        }
    }

    private PresignRequest? ParseRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Body))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PresignRequest>(request.Body);
    }

    private string GetBucketName()
    {
        var bucketName = _configuration["UPLOAD_BUCKET"] ?? _configuration["BUCKET_NAME"];
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("bucket configuration missing");
        }

        return bucketName;
    }

    private string GeneratePreSignedUrl(string bucketName, string fileKey)
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

    private RegionEndpoint GetS3Region()
    {
        var regionName = _configuration["S3_REGION"] ?? _configuration["AWS_REGION"];
        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException("S3 region is not configured. Set S3_REGION or AWS_REGION.");
        }

        return RegionEndpoint.GetBySystemName(regionName);
    }
}
