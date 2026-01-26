using System.Globalization;
using SyncApp26.Shared.DTOs.Response.User;

namespace SyncApp26.Application.Services
{
    public class CsvParserService
    {
        public List<CsvUserDTO> ParseCsv(Stream fileStream)
        {
            var users = new List<CsvUserDTO>();

            using (var reader = new StreamReader(fileStream))
            {
                // Skip header line
                var header = reader.ReadLine();
                if (header == null) return users;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.Length < 6) continue; // Minimum required fields

                    try
                    {
                        var user = new CsvUserDTO
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = values[0].Trim(),
                            DateOfBirth = ParseDate(values[1].Trim()),
                            Department = values[2].Trim(),
                            LineManagerName = values.Length > 3 && !string.IsNullOrWhiteSpace(values[3]) ? values[3].Trim() : null,
                            Role = values.Length > 4 ? values[4].Trim().ToLower() : "employee",
                            Email = values.Length > 5 && !string.IsNullOrWhiteSpace(values[5]) ? values[5].Trim() : null
                        };

                        users.Add(user);
                    }
                    catch (Exception ex)
                    {
                        // Log parsing error but continue
                        Console.WriteLine($"Error parsing line: {line}. Error: {ex.Message}");
                    }
                }
            }

            return users;
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue);
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }

            values.Add(currentValue);
            return values.ToArray();
        }

        private DateTime ParseDate(string dateString)
        {
            // Try multiple date formats
            string[] formats = {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "MM/dd/yyyy",
                "dd-MM-yyyy",
                "MM-dd-yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }

            // Fallback to default parsing
            if (DateTime.TryParse(dateString, out DateTime fallbackResult))
            {
                return fallbackResult;
            }

            throw new FormatException($"Unable to parse date: {dateString}");
        }
    }
}
