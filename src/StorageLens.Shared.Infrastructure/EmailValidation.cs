using System.Text.RegularExpressions;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// RFC 5322 compliant email validation.
/// Provides strict email validation for security and data integrity.
/// </summary>
public static class EmailValidation
{
    // RFC 5322 simplified pattern (practical for most cases)
    // This regex validates most email formats used in practice while rejecting obviously invalid ones
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates an email address according to RFC 5322 standards.
    /// </summary>
    /// <param name="email">The email address to validate</param>
    /// <returns>True if the email is valid; otherwise false</returns>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Check length constraints
        if (email.Length > 254)  // RFC 5321
            return false;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var localPart = parts[0];
        var domain = parts[1];

        // Local part must be 1-64 characters
        if (localPart.Length is < 1 or > 64)
            return false;

        return EmailPattern.IsMatch(email);
    }

    /// <summary>
    /// Tries to extract and validate multiple email addresses separated by comma or semicolon.
    /// </summary>
    /// <param name="emailList">Comma or semicolon-separated email addresses</param>
    /// <param name="validEmails">Output collection of valid email addresses</param>
    /// <returns>True if at least one valid email was found; otherwise false</returns>
    public static bool TryExtractValidEmails(string? emailList, out List<string> validEmails)
    {
        validEmails = new List<string>();

        if (string.IsNullOrWhiteSpace(emailList))
            return false;

        var separators = new[] { ',', ';' };
        var emails = emailList.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var email in emails)
        {
            var trimmed = email.Trim();
            if (IsValidEmail(trimmed))
            {
                validEmails.Add(trimmed);
            }
        }

        return validEmails.Count > 0;
    }
}
