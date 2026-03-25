using Microsoft.AspNetCore.Http;

namespace Shared;

public interface IMediaService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName);
}