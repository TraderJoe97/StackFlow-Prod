using System.Text.RegularExpressions;

namespace StackFlow.Utils
{
    public class PasswordValidator
    {
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // Regex explanation:
                // ^                     : Start of the string.
                // [a-zA-Z0-9@$!%*?&]+  : Matches one or more of the allowed characters.
                //                       - a-zA-Z : Any letter (uppercase or lowercase)
                //                       - 0-9    : Any digit
                //                       - @$!%*?& : Specific allowed special characters.
                // {5,}                : Quantifier. Ensures the entire matched string (password)
                //                       is 5 or more characters long. This means "more than 4".
                // $                     : End of the string.
                string passwordRegex = @"^[a-zA-Z0-9@$!%*?&]{5,}$";

                // We don't typically need IgnoreCase for passwords, but it doesn't hurt.
                // Compiled is good for performance if called frequently.
                return Regex.IsMatch(password, passwordRegex, RegexOptions.Compiled);
            }
            catch (RegexMatchTimeoutException)
            {
                // Log this if needed.
                return false;
            }
            catch (System.ArgumentException)
            {
                // Indicates an issue with the regex pattern itself.
                return false;
            }
        }

    }
}


