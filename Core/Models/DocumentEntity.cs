using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Documents")]
public class DocumentEntity
{
    [DynamoDBHashKey]
    public required string DocumentId { get; set; }

    public required string FileName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public required string FileStatus { get; set; }
}