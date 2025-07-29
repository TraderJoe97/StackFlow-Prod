using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StackFlow.Utils
{
    public static class EmailTemplateHelper
    {
        public static async Task<string> LoadTemplateAndPopulateAsync(string templateFileName, Dictionary<string, string> placeholders)
        {
            var baseDirectory = AppContext.BaseDirectory;
            // Adjust the path to point to the EmailTemplates folder
            var templatePath = Path.Combine(baseDirectory, "EmailTemplates", templateFileName);

            if (!File.Exists(templatePath))
            {
                // Log error or handle missing template appropriately
                Console.WriteLine($"Email template not found: {templatePath}");
                return "Email template not found."; // Basic fallback
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);

            foreach (var placeholder in placeholders)
            {
                templateContent = templateContent.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            return templateContent;
        }
    }
}