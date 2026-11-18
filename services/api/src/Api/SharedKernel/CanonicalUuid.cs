using System.Text.RegularExpressions;

namespace Api.SharedKernel;

public static partial class CanonicalUuid
{
    public static bool TryParse(string? value, out Guid id)
    {
        id = Guid.Empty;
        return value is not null && CanonicalUuidRegex().IsMatch(value) && Guid.TryParse(value, out id);
    }

    public static string Format(Guid id) => id.ToString("D").ToLowerInvariant();

    [GeneratedRegex(@"\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z")]
    private static partial Regex CanonicalUuidRegex();
}
