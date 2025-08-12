#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Services;

public static class BrightColorExtensions {

    private const float TOLERANCE = 0.001f;

    #region Statics members declarations

    public static Rgb ToRgb(this BrightColor color) {
        ArgumentNullException.ThrowIfNull(color);

        string hex = color.ToString();

        if (!hex.StartsWith("#") || hex.Length != 7) { throw new ArgumentException("Invalid BrightColor format", nameof(color)); }

        byte r = Convert.ToByte(hex.Substring(1, 2), 16);
        byte g = Convert.ToByte(hex.Substring(3, 2), 16);
        byte b = Convert.ToByte(hex.Substring(5, 2), 16);

        return new Rgb(r, g, b);
    }

    public static Hsv ToHsv(this BrightColor color) {
        ArgumentNullException.ThrowIfNull(color);

        Rgb rgb = color.ToRgb();
        Hsv hsv = RgbToHsv(rgb);

        return hsv;
    }

    public static BrightColor LerpTo(this BrightColor from, BrightColor to, float t) {
        t = Math.Clamp(t, 0f, 1f);

        // Convert RGB → HSV
        Hsv hsvFrom = from.ToHsv();
        Hsv hsvTo   = to.ToHsv();

        // Interpolate in HSV space
        Hsv hsvResult = hsvFrom.LerpTo(hsvTo, t);

        // Convert HSV → RGB
        Rgb rgb = hsvResult.ToRgb();

        return new BrightColor(rgb.Red, rgb.Green, rgb.Blue);
    }

    public static BrightColor GetPastel(this BrightColor color) {
        ArgumentNullException.ThrowIfNull(color);

        Hsv hsv    = color.ToHsv();
        Hsv newHsv = hsv.With(0.5f, 0.7f);

        return ColorService.GetBrightFromHsv(newHsv);
    }

    private static Hsv RgbToHsv(Rgb rgb) {
        float rNorm = rgb.Red   / 255f;
        float gNorm = rgb.Green / 255f;
        float bNorm = rgb.Blue  / 255f;

        float max   = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        float min   = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        float delta = max - min;

        float h = 0;
        if (delta > 0) {
            if (Math.Abs(max - rNorm) < TOLERANCE) {
                h = 60 * (((gNorm - bNorm) / delta + 6) % 6);
            } else if (Math.Abs(max - gNorm) < TOLERANCE) {
                h = 60 * ((bNorm - rNorm) / delta + 2);
            } else {
                h = 60 * ((rNorm - gNorm) / delta + 4);
            }
        }

        float s = max == 0 ? 0 : delta / max;
        float v = max;

        return new Hsv(h, s, v);
    }

    #endregion

}