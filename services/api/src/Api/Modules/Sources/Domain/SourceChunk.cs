namespace Api.Modules.Sources.Domain;

public sealed class SourceChunk
{
    private SourceChunk()
    {
        HeadingPath = [];
        Text = null!;
    }

    private SourceChunk(Guid id, SourceVersion version, ChunkDraft draft)
    {
        Id = id;
        CampaignId = version.CampaignId;
        SourceVersionId = version.Id;
        Ordinal = draft.Ordinal;
        HeadingPath = [.. draft.HeadingPath];
        StartLine = draft.StartLine;
        EndLine = draft.EndLine;
        Text = draft.Text;
    }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid SourceVersionId { get; private set; }
    public int Ordinal { get; private set; }
    public string[] HeadingPath { get; private set; }
    public int StartLine { get; private set; }
    public int EndLine { get; private set; }
    public string Text { get; private set; }

    public static SourceChunk Create(SourceVersion version, ChunkDraft draft) => new(Guid.NewGuid(), version, draft);
}

public sealed record ChunkDraft(int Ordinal, IReadOnlyList<string> HeadingPath, int StartLine, int EndLine, string Text);
