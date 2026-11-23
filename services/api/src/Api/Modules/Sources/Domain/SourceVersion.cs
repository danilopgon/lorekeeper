namespace Api.Modules.Sources.Domain;

public sealed class SourceVersion
{
    private SourceVersion()
    {
        OriginalText = null!;
        ContentHash = null!;
    }

    private SourceVersion(Guid id, Source source, SourceText text, string? fileName, string? frontmatter, DateTimeOffset now)
    {
        Id = id;
        CampaignId = source.CampaignId;
        SourceId = source.Id;
        OriginalText = text.Value;
        ContentHash = text.ContentHash;
        FileName = fileName;
        Frontmatter = frontmatter;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid SourceId { get; private set; }
    public string OriginalText { get; private set; }
    public string ContentHash { get; private set; }
    public string? Frontmatter { get; private set; }
    public string? FileName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static SourceVersion Create(Source source, SourceText text, string? fileName, DateTimeOffset now) =>
        new(Guid.NewGuid(), source, text, fileName, frontmatter: null, now.ToUniversalTime());

    /// <summary>A version from an uploaded file; Markdown keeps its YAML frontmatter, which is stored but never chunked.</summary>
    public static SourceVersion FromFile(Source source, SourceUpload upload, DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            source,
            upload.Text,
            upload.FileName,
            upload.Format == SourceFormat.Markdown ? Chunking.MarkdownSyntax.Frontmatter(upload.Text.Value) : null,
            now.ToUniversalTime());
}
