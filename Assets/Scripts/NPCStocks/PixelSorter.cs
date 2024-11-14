using UnityEngine;

public class PixelSorter : MonoBehaviour
{
    public enum SortMode
    {
        White,
        Black,
        Bright,
        Dark
    }

    public SortMode mode = SortMode.White;

    // Threshold values (can be adjusted in the Inspector)
    [Header("Thresholds")]
    public int whiteValue = -12345678;
    public int blackValue = -3456789;
    public float brightValue = 0.5f; // Brightness ranges from 0 to 1
    public float darkValue = 0.7f;   // Brightness ranges from 0 to 1

    /// <summary>
    /// Sorts the pixels of the given texture based on the selected mode.
    /// </summary>
    /// <param name="inputTexture">The texture to sort.</param>
    /// <returns>A new sorted texture.</returns>
    public Texture2D SortTexture(Texture2D inputTexture)
    {
        Texture2D sortedTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBA32, false);
        sortedTexture.SetPixels(inputTexture.GetPixels());

        // Sort rows
        for (int y = 0; y < sortedTexture.height; y++)
        {
            Color[] row = sortedTexture.GetPixels(0, y, sortedTexture.width, 1);
            Color[] sortedRow = SortPixels(row, mode);
            sortedTexture.SetPixels(0, y, sortedTexture.width, 1, sortedRow);
        }

        // Sort columns
        for (int x = 0; x < sortedTexture.width; x++)
        {
            Color[] column = new Color[sortedTexture.height];
            for (int y = 0; y < sortedTexture.height; y++)
            {
                column[y] = sortedTexture.GetPixel(x, y);
            }

            Color[] sortedColumn = SortPixels(column, mode);
            for (int y = 0; y < sortedTexture.height; y++)
            {
                sortedTexture.SetPixel(x, y, sortedColumn[y]);
            }
        }

        sortedTexture.Apply();
        return sortedTexture;
    }

    /// <summary>
    /// Sorts an array of pixels based on the selected mode.
    /// </summary>
    /// <param name="pixels">The array of pixels to sort.</param>
    /// <param name="mode">The sorting mode.</param>
    /// <returns>A new sorted array of pixels.</returns>
    private Color[] SortPixels(Color[] pixels, SortMode mode)
    {
        // Determine sorting criteria based on mode
        switch (mode)
        {
            case SortMode.White:
                return SortByCondition(pixels, c => ColorToInt(c) >= whiteValue);
            case SortMode.Black:
                return SortByCondition(pixels, c => ColorToInt(c) <= blackValue);
            case SortMode.Bright:
                return SortByCondition(pixels, c => c.grayscale >= brightValue);
            case SortMode.Dark:
                return SortByCondition(pixels, c => c.grayscale <= darkValue);
            default:
                return pixels;
        }
    }

    /// <summary>
    /// Converts a Color to an integer representation.
    /// </summary>
    /// <param name="c">The color to convert.</param>
    /// <returns>An integer representing the color.</returns>
    private int ColorToInt(Color c)
    {
        return (int)(c.r * 255) << 16 | (int)(c.g * 255) << 8 | (int)(c.b * 255);
    }

    /// <summary>
    /// Sorts pixels that meet a certain condition.
    /// </summary>
    /// <param name="pixels">The array of pixels.</param>
    /// <param name="condition">The condition to sort by.</param>
    /// <returns>A sorted array of pixels.</returns>
    private Color[] SortByCondition(Color[] pixels, System.Func<Color, bool> condition)
    {
        // Find sequences to sort
        int start = 0;
        while (start < pixels.Length)
        {
            // Find the start of a sortable sequence
            while (start < pixels.Length && !condition(pixels[start]))
                start++;

            if (start >= pixels.Length)
                break;

            // Find the end of the sortable sequence
            int end = start + 1;
            while (end < pixels.Length && condition(pixels[end]))
                end++;

            // Sort the sequence between start and end
            if (end > start + 1)
            {
                System.Array.Sort(pixels, start, end - start, new ColorComparer(mode));
            }

            start = end;
        }

        return pixels;
    }

    /// <summary>
    /// Custom comparer for sorting colors based on the selected mode.
    /// </summary>
    private class ColorComparer : System.Collections.Generic.IComparer<Color>
    {
        private SortMode mode;

        public ColorComparer(SortMode mode)
        {
            this.mode = mode;
        }

        public int Compare(Color a, Color b)
        {
            switch (mode)
            {
                case SortMode.White:
                    return ColorToInt(a).CompareTo(ColorToInt(b));
                case SortMode.Black:
                    return ColorToInt(a).CompareTo(ColorToInt(b));
                case SortMode.Bright:
                    return a.grayscale.CompareTo(b.grayscale);
                case SortMode.Dark:
                    return a.grayscale.CompareTo(b.grayscale);
                default:
                    return 0;
            }
        }

        private int ColorToInt(Color c)
        {
            return (int)(c.r * 255) << 16 | (int)(c.g * 255) << 8 | (int)(c.b * 255);
        }
    }
}
