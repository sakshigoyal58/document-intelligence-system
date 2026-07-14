using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.Extensions.Logging;

namespace Services.TextractServices;

public class TextractService : ITextractService
{
    private readonly IAmazonTextract _textractClient;
    private readonly ILogger<TextractService> _logger;
    private readonly string _bucketName;
    private readonly string _snsTopicArn;
    private readonly string _snsRoleArn;

    public TextractService(IAmazonTextract textractClient, ILogger<TextractService> logger)
    {
        _textractClient = textractClient;
        _logger = logger;

        _bucketName = Environment.GetEnvironmentVariable("UPLOAD_BUCKET")
            ?? throw new InvalidOperationException("UPLOAD_BUCKET not configured.");
        _snsTopicArn = Environment.GetEnvironmentVariable("TEXTRACT_SNS_TOPIC_ARN")
            ?? throw new InvalidOperationException("TEXTRACT_SNS_TOPIC_ARN not configured.");
        _snsRoleArn = Environment.GetEnvironmentVariable("TEXTRACT_SNS_ROLE_ARN")
            ?? throw new InvalidOperationException("TEXTRACT_SNS_ROLE_ARN not configured.");
    }

    public async Task<string> StartTextDetectionJobAsync(string s3Key)
    {
        var request = new StartDocumentTextDetectionRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new Amazon.Textract.Model.S3Object
                {
                    Bucket = _bucketName,
                    Name = s3Key
                }
            },
            NotificationChannel = new NotificationChannel
            {
                SNSTopicArn = _snsTopicArn,
                RoleArn = _snsRoleArn
            }
        };

        var response = await _textractClient.StartDocumentTextDetectionAsync(request);

        _logger.LogInformation(
            "Started Textract job {JobId} for key {S3Key}",
            response.JobId, s3Key);

        return response.JobId;
    }
}