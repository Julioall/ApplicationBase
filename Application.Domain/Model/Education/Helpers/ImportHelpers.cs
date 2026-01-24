using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.Domain.Model.Education.Helpers
{
    public static class TextNormalizer
    {
        public static string NormalizeKey(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return Regex.Replace(
                text.Trim().ToLowerInvariant(),
                @"\s+",
                " "
            );
        }

        public static string NormalizeCpf(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;

            return Regex.Replace(cpf, @"[^\d]", "");
        }

        public static string NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            return email.Trim().ToLowerInvariant();
        }

        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            return Regex.Replace(phone, @"[^\d+]", "").Trim();
        }
    }

    public static class ClassNameParser
    {
        public static (string classCode, string programName, string classSuffix) ParseClassName(string courseColumn)
        {
            if (string.IsNullOrWhiteSpace(courseColumn))
                return (string.Empty, string.Empty, string.Empty);

            var trimmed = courseColumn.Trim();
            var parts = trimmed.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            if (parts.Count < 2)
                return (trimmed, string.Empty, string.Empty);

            var classCode = parts[0];
            var programName = parts.Count >= 2 ? parts[1] : string.Empty;
            var classSuffix = parts.Count >= 3 ? parts[2] : string.Empty;

            return (classCode, programName, classSuffix);
        }
    }

    public static class DateParser
    {
        public static DateTime? ParseExcelDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            var trimmed = dateStr.Trim();

            // Tentar parse como número (Excel serial date)
            if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var excelDate))
            {
                try
                {
                    var date = DateTime.FromOADate((double)excelDate);
                    return date;
                }
                catch
                {
                    // Não é data Excel válida, continuar
                }
            }

            // Tentar parse como string de data
            var dateFormats = new[]
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd/MM/yyyy HH:mm",
                "yyyy-MM-dd HH:mm"
            };

            foreach (var format in dateFormats)
            {
                if (DateTime.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }

            return null;
        }

        public static long? ConvertToUnixTimestamp(DateTime? date)
        {
            if (!date.HasValue)
                return null;

            return (long)(date.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }

    public static class GradeParser
    {
        public static decimal? ParseGrade(string? gradeStr)
        {
            if (string.IsNullOrWhiteSpace(gradeStr))
                return null;

            var trimmed = gradeStr.Trim();

            if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var grade))
            {
                if (grade >= 0 && grade <= 10)
                    return grade;
            }

            return null;
        }
    }
}
