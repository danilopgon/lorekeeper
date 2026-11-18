using System.Text.Json.Serialization;

namespace Api.Modules.Sources.Application;

public sealed record SourceDto(
    string Id,
    string CampaignId,
    string Title,
    SourceOriginDto Origin,
    SourceCategoryDto? Category,
    KnowledgeStatusDto KnowledgeStatus,
    SourceAvailabilityDto Availability,
    DateTimeOffset? LastPublishedAt,
    SourceProcessingDto? Processing,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SourceProcessingDto(SourceProcessingStateDto State, string? ErrorCode, int Attempts);

[JsonConverter(typeof(JsonStringEnumConverter<SourceOriginDto>))]
public enum SourceOriginDto
{
    [JsonStringEnumMemberName("file")] File,
    [JsonStringEnumMemberName("paste")] Paste
}

[JsonConverter(typeof(JsonStringEnumConverter<SourceCategoryDto>))]
public enum SourceCategoryDto
{
    [JsonStringEnumMemberName("session_notes")] SessionNotes,
    [JsonStringEnumMemberName("preparation")] Preparation,
    [JsonStringEnumMemberName("characters_places")] CharactersPlaces,
    [JsonStringEnumMemberName("general_reference")] GeneralReference
}

[JsonConverter(typeof(JsonStringEnumConverter<KnowledgeStatusDto>))]
public enum KnowledgeStatusDto
{
    [JsonStringEnumMemberName("unknown")] Unknown,
    [JsonStringEnumMemberName("preparation")] Preparation,
    [JsonStringEnumMemberName("occurred")] Occurred
}

[JsonConverter(typeof(JsonStringEnumConverter<SourceAvailabilityDto>))]
public enum SourceAvailabilityDto
{
    [JsonStringEnumMemberName("available")] Available,
    [JsonStringEnumMemberName("unavailable")] Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter<SourceProcessingStateDto>))]
public enum SourceProcessingStateDto
{
    [JsonStringEnumMemberName("queued")] Queued,
    [JsonStringEnumMemberName("processing")] Processing,
    [JsonStringEnumMemberName("failed")] Failed
}
