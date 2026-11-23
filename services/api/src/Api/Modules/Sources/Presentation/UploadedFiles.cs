using Api.Modules.Sources.Application;
using Api.Modules.Sources.Domain;

namespace Api.Modules.Sources.Presentation;

internal static class UploadedFiles
{
    public static async Task<IReadOnlyList<UploadedFile>> ReadAllAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var read = new List<UploadedFile>();
        foreach (var file in files)
        {
            read.Add(await ReadAsync(file, cancellationToken));
        }

        return read;
    }

    /// <summary>Reads a file part into memory unless it is over the per-file limit, which is then reported without reading it.</summary>
    private static async Task<UploadedFile> ReadAsync(IFormFile file, CancellationToken cancellationToken)
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
