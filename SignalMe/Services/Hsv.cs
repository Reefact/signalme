#region Usings declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Value;

#endregion

namespace SignalMe.Services;

/// <summary>
///     Represents a color using the HSV (Hue, Saturation, Value) color space.
///     Hue corresponds to the position on the color wheel, expressed in degrees from 0 to 360 (excluded).
///     Saturation defines how pure or intense the color is, from 0 (gray) to 1 (fully saturated).
///     Value defines the brightness of the color, from 0 (black) to 1 (full brightness).
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class Hsv : ValueType<Hsv> {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="Hsv" /> class.
    /// </summary>
    /// <param name="hue">Hue in degrees (0 ≤ hue &lt; 360).</param>
    /// <param name="saturation">Saturation in [0, 1].</param>
    /// <param name="value">Brightness in [0, 1].</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any value is out of range.</exception>
    public Hsv(float hue, float saturation, float value) {
        if (hue        < 0 || hue        >= 360) { throw new ArgumentOutOfRangeException(nameof(hue), "Hue must be in [0, 360)."); }
        if (saturation < 0 || saturation > 1) { throw new ArgumentOutOfRangeException(nameof(saturation), "Saturation must be in [0, 1]."); }
        if (value      < 0 || value      > 1) { throw new ArgumentOutOfRangeException(nameof(value), "Value must be in [0, 1]."); }

        Hue        = hue;
        Saturation = saturation;
        Value      = value;
    }

    #endregion

    /// <summary>
    ///     Gets the hue component, in degrees [0, 360).
    /// </summary>
    public float Hue { get; }

    /// <summary>
    ///     Gets the saturation component, in [0, 1].
    /// </summary>
    public float Saturation { get; }

    /// <summary>
    ///     Gets the value (brightness) component, in [0, 1].
    /// </summary>
    public float Value { get; }

    public Hsv With(float newSaturation, float newValue) {
        return new Hsv(Hue, newSaturation, newValue);
    }

    public Rgb ToRgb() {
        double c = Value * Saturation;
        double x = c     * (1 - Math.Abs(Hue / 60.0 % 2 - 1));
        double m = Value - c;

        double r1 = 0, g1 = 0, b1 = 0;
        if (Hue < 60) {
            r1 = c;
            g1 = x;
            b1 = 0;
        } else if (Hue < 120) {
            r1 = x;
            g1 = c;
            b1 = 0;
        } else if (Hue < 180) {
            r1 = 0;
            g1 = c;
            b1 = x;
        } else if (Hue < 240) {
            r1 = 0;
            g1 = x;
            b1 = c;
        } else if (Hue < 300) {
            r1 = x;
            g1 = 0;
            b1 = c;
        } else {
            r1 = c;
            g1 = 0;
            b1 = x;
        }

        byte r = (byte)Math.Round((r1 + m) * 255);
        byte g = (byte)Math.Round((g1 + m) * 255);
        byte b = (byte)Math.Round((b1 + m) * 255);

        return new Rgb(r, g, b);
    }

    public Hsv LerpTo(Hsv to, float t) {
        // Clamp t to [0,1] to avoid overshoot
        t = Math.Clamp(t, 0f, 1f);

        // Hue interpolation with wrap-around handling (circular hue space)
        float deltaHue = to.Hue - Hue;
        if (Math.Abs(deltaHue) > 180f) {
            deltaHue -= MathF.Sign(deltaHue) * 360f;
        }

        float hue = (Hue + deltaHue * t) % 360f;
        if (hue < 0f) {
            hue += 360f;
        }

        float saturation = Saturation + (to.Saturation - Saturation) * t;
        float value      = Value      + (to.Value      - Value)      * t;

        return new Hsv(hue, saturation, value);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"HSV({Hue:0.##}, {Saturation:0.##}, {Value:0.##})";
    }

    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Hue;
        yield return Saturation;
        yield return Value;
    }

}