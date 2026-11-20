using System.ComponentModel.DataAnnotations;

namespace MEMS.Data.Services;

public sealed class ApiFault(int status, string message, IDictionary<string,string[]>? errors = null) : Exception(message)
{
    public int Status { get; } = status;
    public IDictionary<string,string[]>? Errors { get; } = errors;
}

public static class ContractValidation
{
    public static void Check(object value)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), results, true))
            throw new ApiFault(400, "The request contains invalid fields.", results.SelectMany(r => r.MemberNames.DefaultIfEmpty("request").Select(n => (n, r.ErrorMessage ?? "Invalid value.")))
                .GroupBy(x => x.n).ToDictionary(x => x.Key, x => x.Select(y => y.Item2).ToArray()));
    }

    public static void Version(byte[] actual, string? header)
    {
        if (header is null || header.Length < 2 || !header.StartsWith('"') || !header.EndsWith('"')) throw new ApiFault(400, "Supply the current quoted version in If-Match.");
        byte[] expected;
        try { expected = Convert.FromBase64String(header[1..^1]); } catch (FormatException) { throw new ApiFault(400, "If-Match must contain a valid version."); }
        if (expected.Length != 8) throw new ApiFault(400, "If-Match must contain a valid version.");
        if (!actual.AsSpan().SequenceEqual(expected)) throw new ApiFault(409, "The record changed. Reload it and retry.");
    }
}
