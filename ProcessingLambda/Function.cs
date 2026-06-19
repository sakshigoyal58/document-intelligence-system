using Amazon.Lambda.Core;
using Services.DynamoDb;
using Business.Validation;
using Amazon.Lambda.S3Events;
using static Amazon.Lambda.S3Events.S3Event;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProcessingLambda;

public class Function
{
    private readonly IDynamoDbService _dynamoDBService;
    private readonly IPdfValidator _validator;

    public Function()
    : this(new DynamoDbService(), new PdfValidator())
    {}   

    public Function(IDynamoDbService dynamoDBService, IPdfValidator validator)
    {
        _dynamoDBService = dynamoDBService;
        _validator = validator;
    }

    public async Task FunctionHandler(S3Event s3Event, ILambdaContext context)
{
    try
    {
        foreach (var record in s3Event.Records)
        {
            await ProcessFile(record, context);
        }
    }
    catch (Exception ex)
    {
        context.Logger.LogLine($"Error: {ex.Message}");
        throw;
    }
}   
    private async Task ProcessFile(S3EventNotificationRecord record, ILambdaContext context)
    {
    var fileName = record.S3.Object.Key;
    var fileSize = record.S3.Object.Size;
    var fileId = Guid.NewGuid().ToString();

    context.Logger.LogLine($"Processing: {fileName} ({fileSize} bytes)");

    await _dynamoDBService.AddFileRecordAsync(fileId, fileName, fileSize);
    context.Logger.LogLine($"✓ Record created: {fileId}");

    var validation = _validator.Validate(fileName, fileSize);

    if (validation.IsValid)
    {
        await _dynamoDBService.UpdateFileStatusAsync(fileId, "VALIDATED");
        context.Logger.LogLine($"✓ Validated: {fileId}");
    }
    else
    {
        await _dynamoDBService.UpdateFileStatusAsync(fileId, "VALIDATION_FAILED", validation.Error);
        context.Logger.LogLine($"✗ Failed: {validation.Error}");
    }
    }
}