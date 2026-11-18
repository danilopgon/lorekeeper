namespace Api.SharedKernel;

public static class UnicodeScalars
{
    public static int Count(string value)
    {
        var count = 0;

        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }
}
