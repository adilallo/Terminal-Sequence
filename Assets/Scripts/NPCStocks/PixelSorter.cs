using UnityEngine;

public class PixelSorter : MonoBehaviour
{
    public enum SortMode { White, Black, Bright, Dark }
    public SortMode mode = SortMode.White;

    [Header("Thresholds")]
    public uint whiteValue = 0xF0F0F0;   // packed RGB threshold
    public uint blackValue = 0x202020;
    public byte brightValue = 128;       // 0-255 grayscale
    public byte darkValue = 180;       // 0-255 grayscale

    /* ─── public API ─────────────────────────────────────────────── */

    public Texture2D SortTexture(Texture2D src, bool clone = true)
    {
        // 1️⃣ guarantee we’re working on a readable RGBA32 texture
        Texture2D work = src;

        bool needsCopy =
            !src.isReadable ||
            !(src.format == TextureFormat.RGBA32 || src.format == TextureFormat.ARGB32);

        if (needsCopy || clone)
        {
            work = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            work.SetPixels32(src.GetPixels32());     // this will throw only if src unreadable
            work.Apply(false, false);
        }

        // 2️⃣ sort in-place on the Color32 buffer
        var pix = work.GetPixels32();
        int w = work.width, h = work.height;

        Sort2D(pix, w, h, true);   // rows
        Sort2D(pix, h, w, false);  // columns (transpose trick)

        work.SetPixels32(pix);
        work.Apply(false, !clone);  // make non-readable if we didn’t clone
        return work;
    }

    /* ─── internal helpers ───────────────────────────────────────── */

    void Sort2D(Color32[] pix, int w, int h, bool rowMajor)
    {
        // scratch arrays reused each segment
        int maxLen = Mathf.Max(w, h);
        var segment = new Color32[maxLen];
        var keyBytes = new uint[maxLen];                // byte or uint depending mode

        for (int y = 0; y < h; y++)
        {
            int idxLine = rowMajor ? y * w : y;    // base index for this line
            int stride = rowMajor ? 1 : w;        // stride between pixels

            int x = 0;
            while (x < w)
            {
                int idx = idxLine + x * stride;
                if (idx >= pix.Length) break;

                while (x < w && idx < pix.Length && !Meets(pix[idx]))
                {
                    x++;
                    idx = idxLine + x * stride;
                }
                if (x >= w || idx >= pix.Length) break;

                // collect run
                int run = 0;
                while (x + run < w)
                {
                    int i = idxLine + (x + run) * stride;
                    if (i >= pix.Length || !Meets(pix[i]))
                        break;

                    var col = pix[i];
                    segment[run] = col;
                    keyBytes[run] = SortKey(col);
                    run++;
                }

                // sort run by key
                System.Array.Sort(keyBytes, segment, 0, run);

                // write back
                for (int i = 0; i < run; i++)
                    pix[idxLine + (x + i) * stride] = segment[i];

                x += run;
            }
        }
    }

    /* ─── condition & key helpers ───────────────────────────────── */

    bool Meets(Color32 c)
    {
        switch (mode)
        {
            case SortMode.White: return Packed(c) >= whiteValue;
            case SortMode.Black: return Packed(c) <= blackValue;
            case SortMode.Bright: return Gray(c) >= brightValue;
            case SortMode.Dark: return Gray(c) <= darkValue;
            default: return false;
        }
    }

    uint SortKey(Color32 c)
    {
        return mode switch
        {
            SortMode.White or SortMode.Black => Packed(c),
            _ => Gray(c),
        };
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static uint Packed(Color32 c) => (uint)(c.r << 16 | c.g << 8 | c.b);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static byte Gray(Color32 c) => (byte)((c.r * 299 + c.g * 587 + c.b * 114 + 500) / 1000); // perceptual luma
}
