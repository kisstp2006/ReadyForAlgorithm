using System.Text;

namespace ReadyForAlgorithm.Core;

public static class MapLoader
{
    public static char[,] LoadGrid(string[] lines)
    {
        if (lines.Length == 0)
        {
            throw new InvalidOperationException("A map fajl ures.");
        }

        int height = lines.Length;
        int width = lines[0].Split(',').Length;
        char[,] grid = new char[height, width];

        for (int y = 0; y < height; y++)
        {
            string[] cells = lines[y].Split(',');
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = cells[x].Trim()[0];
            }
        }

        return grid;
    }

    public static string[] LoadLines(string? path = null)
    {
        string resolvedPath = ResolveMapPath(path);
        if (File.Exists(resolvedPath))
        {
            return File.ReadAllLines(resolvedPath, Encoding.UTF8);
        }

        return GetFallbackMap();
    }

    public static string ResolveMapPath(string? path = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.GetFullPath(path);
        }

        string baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDirectory, "mars_map_50x50.csv"),
            Path.Combine(baseDirectory, "..", "..", "..", "mars_map_50x50.csv"),
            Path.Combine(baseDirectory, "..", "..", "..", "..", "mars_map_50x50.csv")
        };

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return Path.GetFullPath(candidates[0]);
    }

    private static string[] GetFallbackMap()
    {
        return new[]
        {
            "S,.,.,.,#,.,.,.,.,.",
            "#,#,.,.,#,.,#,#,#,.",
            ".,.,.,.,.,.,.,.,#,.",
            ".,#,#,#,#,#,.,.,#,.",
            ".,.,.,G,.,.,.,.,#,.",
            ".,#,.,#,#,#,#,.,#,.",
            ".,#,.,.,.,.,#,.,.,.",
            ".,#,#,#,.,.,#,Y,#,.",
            ".,.,.,#,.,.,.,.,#,B",
            ".,.,.,#,.,#,#,.,.,."
        };
    }
}