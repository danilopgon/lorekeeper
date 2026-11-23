namespace Api.Modules.Sources.Application;

/// <summary>An uploaded file already read by Presentation; <see cref="Content"/> is null when the file was too large to read.</summary>
public sealed record UploadedFile(string FileName, long Length, byte[]? Content);
