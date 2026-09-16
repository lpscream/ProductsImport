namespace ProductsImport.Services;

/// <summary>Validates EAN-8/EAN-13 barcodes and generates unique internal EAN-13 codes.</summary>
public static class BarcodeGenerator
{
    /// <summary>Prefix range 20-29 is reserved by the GS1 standard for internal/in-store use.</summary>
    private const string InternalPrefix = "20";

    public static bool IsValidBarcode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length is not (8 or 12 or 13))
        {
            return false;
        }

        return trimmed.All(char.IsDigit);
    }

    /// <summary>Generates a random EAN-13 barcode not present in <paramref name="existing"/>, and reserves it.</summary>
    public static string GenerateUniqueEan13(HashSet<string> existing, Random random)
    {
        string candidate;
        do
        {
            var body = InternalPrefix + random.Next(0, 100_000_000).ToString("D9");
            var check = ComputeEan13CheckDigit(body);
            candidate = body + check;
        } while (existing.Contains(candidate));

        existing.Add(candidate);
        return candidate;
    }

    /// <summary>Standard GS1 mod-10 check digit for the first 12 digits of an EAN-13 code.</summary>
    public static char ComputeEan13CheckDigit(string first12Digits)
    {
        if (first12Digits.Length != 12 || !first12Digits.All(char.IsDigit))
        {
            throw new ArgumentException("Expected exactly 12 digits.", nameof(first12Digits));
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return (char)('0' + checkDigit);
    }
}
