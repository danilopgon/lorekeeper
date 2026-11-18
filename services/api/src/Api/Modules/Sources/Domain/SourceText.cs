using System.Security.Cryptography;
using System.Text;
using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public sealed record SourceText
{
    public const int MaxBytes = 1_048_576;
    public const string EmptyCode = "source_empty";
    public const string TooLargeCode = "source_too_large";
    public const string EncodingInvalidCode = "source_encoding_invalid";

    private const char ByteOrderMark = '﻿';
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private SourceText(string value, string contentHash)
    {
        Value = value;
        ContentHash = contentHash;
    }

    public string Value { get; }

    public string ContentHash { get; }

    public static Result<SourceText> Create(string? value)
    {
        var text = value is not null && value.StartsWith(ByteOrderMark) ? value[1..] : value;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Error.Validation(EmptyCode, "Source text is empty.");
        }

        if (text.Contains('\0', StringComparison.Ordinal))
        {
            return Error.Validation(EncodingInvalidCode, "Source text cannot be stored as UTF-8 text.");
        }

        byte[] bytes;
        try
        {
            bytes = StrictUtf8.GetBytes(text);
        }
        catch (EncoderFallbackException)
        {
            return Error.Validation(EncodingInvalidCode, "Source text cannot be stored as UTF-8 text.");
        }

        if (bytes.Length > MaxBytes)
        {
            return Error.Validation(TooLargeCode, "Source text is larger than 1 MiB.");
        }

        return new SourceText(text, Hash(bytes));
    }

    private static string Hash(byte[] utf8) => Convert.ToHexStringLower(SHA256.HashData(utf8));
}
