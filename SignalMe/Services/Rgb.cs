#region Usings declarations

using System.Diagnostics;

#endregion

namespace SignalMe.Services;

/// <summary>
///     Represents a color in the RGB (Red, Green, Blue) color model,
///     where each component is an integer between 0 and 255.
/// </summary>
/// <remarks>
///     Every triple of bytes is a valid color, so there is no invariant for the default value to break:
///     <c>default(Rgb)</c> is black. The properties are get-only rather than <c>init</c>, which keeps the
///     <c>with</c> expression out of reach and the type genuinely immutable.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public readonly record struct Rgb {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="Rgb" /> using the specified red, green, and blue components.
    /// </summary>
    /// <param name="red">The red component (0–255).</param>
    /// <param name="green">The green component (0–255).</param>
    /// <param name="blue">The blue component (0–255).</param>
    public Rgb(byte red, byte green, byte blue) {
        Red   = red;
        Green = green;
        Blue  = blue;
    }

    #endregion

    /// <summary>
    ///     Gets the red component of the color (0–255).
    /// </summary>
    public byte Red { get; }

    /// <summary>
    ///     Gets the green component of the color (0–255).
    /// </summary>
    public byte Green { get; }

    /// <summary>
    ///     Gets the blue component of the color (0–255).
    /// </summary>
    public byte Blue { get; }

    /// <summary>
    ///     Returns the hexadecimal string representation of the RGB color (e.g. "#FF00CC").
    /// </summary>
    /// <returns>A string representing the color in hexadecimal format.</returns>
    public override string ToString() {
        return $"#{Red:X2}{Green:X2}{Blue:X2}";
    }

}
