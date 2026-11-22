using System.IO;
using System.Windows.Media.Imaging;

namespace Boots.Data;

internal static class ProductImages
{
    private const string ImportedPrefix = "local-";
    private const string ResourcePath = "/Boots;component/res/";
    private const string Placeholder = "Чудо Обувь.png";

    private static readonly string ImagesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Boots", "Images");

    public static string GetSource(string? imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile) || imageFile != Path.GetFileName(imageFile))
        {
            return ResourcePath + Placeholder;
        }

        if (!imageFile.StartsWith(ImportedPrefix, StringComparison.Ordinal))
        {
            return ResourcePath + imageFile;
        }

        var file = Path.Combine(ImagesDirectory, imageFile);
        return File.Exists(file) ? new Uri(file).AbsoluteUri : ResourcePath + Placeholder;
    }

    public static BitmapFrame LoadPreview(string path)
    {
        using var stream = File.OpenRead(path);
        return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    }

    public static string CopyImported(string path)
    {
        var name = $"{ImportedPrefix}{Guid.NewGuid():N}{Path.GetExtension(path).ToLowerInvariant()}";
        Directory.CreateDirectory(ImagesDirectory);
        File.Copy(path, Path.Combine(ImagesDirectory, name));
        return name;
    }

    public static void RemoveImported(string? imageFile)
    {
        if (imageFile is null)
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(ImagesDirectory, imageFile));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Keep the database error as the primary error if the copied file cannot be removed.
        }
    }
}
