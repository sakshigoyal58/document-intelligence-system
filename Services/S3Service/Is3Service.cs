using System.IO;
using System.Threading.Tasks;

namespace Services.S3Service;

public interface IS3Service
{
    Task<Stream> DownloadFileFromS3Async(string s3Key);
}