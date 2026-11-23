using Api.Modules.Sources.Application;
using Api.Modules.Sources.Domain;

namespace Api.Modules.Sources.Presentation;

internal static class UploadedFiles
{
    /// <summary>Reads a file part into memory unless it is over the per-file limit, which is then reported without reading it.</summary>
    public static async Task<UploadedFile> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > SourceFormats.MaxFileBytes)
        {
            return new UploadedFile(file.FileName, file.Length, Content: null);
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return new UploadedFile(file.FileName, file.Length, buffer.ToArray());
    }
}
