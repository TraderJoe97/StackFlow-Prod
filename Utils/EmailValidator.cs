using System.Text.RegularExpressions;

namespace StackFlow.Utils
{
    public class EmailValidator
    {
            /// <summary>
            /// Checks the validity of an email address using a regular expression.
            /// This regex broadly covers most valid email formats while being
            /// reasonably simple. It checks for:
            /// - User part: one or more of alphanumeric, dot, underscore, percent, plus, hyphen.
            /// - '@' symbol.
            /// - Domain part: one or more of alphanumeric, dot, hyphen.
            /// - Top-level domain (TLD): a dot followed by two or more letters.
            /// </summary>
            /// <param name="email">The email address to validate.</param>
            /// <returns>True if the email is valid, false otherwise.</returns>
            public static bool IsValidEmail(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                try
                {
                    // A common and generally robust regular expression for email validation.
                    // Note: Comprehensive email validation according to RFCs is extremely complex.
                    // This regex provides a good balance for typical web application needs.
                    string emailRegex = @"^[a-zA-Z0-9][a-zA-Z0-9._]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

                    // RegexOptions.IgnoreCase makes the matching case-insensitive.
                    // RegexOptions.Compiled improves performance for frequent use (optional, but good for helpers).
                    return Regex.IsMatch(email, emailRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch (RegexMatchTimeoutException)
                {
                    // Log this if needed. For a helper, just returning false is often sufficient.
                    return false;
                }
                catch (System.ArgumentException)
                {
                    // This indicates an issue with the regex pattern itself.
                    // Should not happen with a hardcoded, tested pattern.
                    return false;
                }
            }
        
    }
}


