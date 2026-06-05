using Services.DynamoDb;

var fileService = new DynamoDbFileService();

// Test 1: Add Record
string fileId = Guid.NewGuid().ToString();
string fileName = "test-document.pdf";
long fileSize = 1024000;

Console.WriteLine("Adding record...");
await fileService.AddFileRecordAsync(fileId, fileName, fileSize);
Console.WriteLine($"✓ Record added: {fileId}");

// Test 2: Update Status
Console.WriteLine("\nUpdating status to VALIDATED...");
await fileService.UpdateFileStatusAsync(fileId, "VALIDATED");
Console.WriteLine($"✓ Status updated to VALIDATED");

// Test 3: Update with Error
string fileId2 = Guid.NewGuid().ToString();
Console.WriteLine("\nAdding second record...");
await fileService.AddFileRecordAsync(fileId2, "invalid.txt", 500);
Console.WriteLine($"✓ Record added: {fileId2}");

Console.WriteLine("\nUpdating status to VALIDATION_FAILED...");
await fileService.UpdateFileStatusAsync(fileId2, "VALIDATION_FAILED", "File is not PDF");
Console.WriteLine($"✓ Status updated with error");

Console.WriteLine("\n✅ All tests passed!");
