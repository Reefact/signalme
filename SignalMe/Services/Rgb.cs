#region Usings declarations

using System;
using System.Diagnostics;

#endregion

namespace SignalMe.Services;

/// <summary>
///     Represents a color in the RGB (Red, Green, Blue) color model,
///     where each component is an integer between 0 and 255.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class Rgb : IEquatable<Rgb> {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="Rgb" /> class
    ///     using the specified red, green, and blue components.
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

    /// <inheritdoc />
    public bool Equals(Rgb? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return Red == other.Red && Green == other.Green && Blue == other.Blue;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return Equals(obj as Rgb);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            int hashCode = Red;
            hashCode = (hashCode * 397) ^ Green;
            hashCode = (hashCode * 397) ^ Blue;

            return hashCode;
        }
    }

    /// <summary>Indicates whether two <see cref="Rgb">RGB colors</see> are equal.</summary>
    /// <param name="left">The first <see cref="Rgb">RGB color</see> to compare.</param>
    /// <param name="right">The second <see cref="Rgb">RGB color</see> to compare.</param>
    /// <returns>true if both values are equal, otherwise false.</returns>
    public static bool operator ==(Rgb? left, Rgb? right) {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>Indicates whether two <see cref="Rgb">RGB colors</see> are different.</summary>
    /// <param name="left">The first <see cref="Rgb">RGB color</see> to compare.</param>
    /// <param name="right">The second <see cref="Rgb">RGB color</see> to compare.</param>
    /// <returns>true if both values are different, otherwise false.</returns>
    public static bool operator !=(Rgb? left, Rgb? right) {
        return !(left == right);
    }

}