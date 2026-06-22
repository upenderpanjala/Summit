using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Summit.VMS.Validation;

/// <summary>Phone helpers shared by the API, mobile apps (via the same rules), and web.</summary>
public static partial class PhoneNumber
{
    [GeneratedRegex(@"^[6-9]\d{9}$")]
    private static partial Regex IndianMobile();

    /// <summary>
    /// Reduce a number to a bare 10-digit Indian mobile where possible:
    /// strips spaces/dashes/brackets, a leading +91/91, and a leading 0.
    /// Returns the digits as-is if it doesn't fit, so callers can still validate.
    /// </summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91")) digits = digits[2..];
        else if (digits.Length == 11 && digits.StartsWith("0")) digits = digits[1..];
        return digits;
    }

    public static bool IsValidMobile(string? raw) => IndianMobile().IsMatch(Normalize(raw));
}

/// <summary>Validates a 10-digit Indian mobile (after normalization).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IndianMobileAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; }

    public override bool IsValid(object? value)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return AllowEmpty;
        return PhoneNumber.IsValidMobile(s);
    }

    public override string FormatErrorMessage(string name) =>
        $"{name} must be a valid 10-digit mobile number (starting 6-9).";
}

/// <summary>Validates a person's name: 2–60 chars, letters/spaces/.'- only.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed partial class PersonNameAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^[\p{L}][\p{L}\s.'\-]{1,59}$")]
    private static partial Regex NameRx();

    public override bool IsValid(object? value)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim();
        return t.Length is >= 2 and <= 60 && NameRx().IsMatch(t);
    }

    public override string FormatErrorMessage(string name) =>
        $"{name} must be 2–60 letters (spaces, . ' - allowed).";
}

/// <summary>Validates a 6-digit numeric one-time code.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed partial class OtpCodeAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^\d{6}$")]
    private static partial Regex SixDigits();

    public override bool IsValid(object? value)
        => value is string s && SixDigits().IsMatch(s);

    public override string FormatErrorMessage(string name) => $"{name} must be a 6-digit code.";
}
