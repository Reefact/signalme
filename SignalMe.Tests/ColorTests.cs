using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

namespace SignalMe.Tests;

public sealed class ColorTests {

    public static TheoryData<byte, byte, byte> Palette => new() {
        { 0, 0, 0 },       // black
        { 255, 255, 255 }, // white
        { 128, 128, 128 }, // grey
        { 255, 0, 0 }, { 0, 255, 0 }, { 0, 0, 255 },
        { 255, 255, 0 }, { 0, 255, 255 }, { 255, 0, 255 },
        { 153, 50, 204 }, // the "away" purple
        { 1, 2, 3 }, { 254, 253, 252 }
    };

    #region value semantics

    [Fact]
    public void Two_rgb_of_the_same_components_are_equal() {
        Check.That(new Rgb(1, 2, 3)).IsEqualTo(new Rgb(1, 2, 3));
        Check.That(new Rgb(1, 2, 3) == new Rgb(1, 2, 3)).IsTrue();
        Check.That(new Rgb(1, 2, 3) != new Rgb(1, 2, 3)).IsFalse();
        Check.That(new Rgb(3, 2, 1)).IsNotEqualTo(new Rgb(1, 2, 3));
        Check.That(new Rgb(1, 2, 3).GetHashCode()).IsEqualTo(new Rgb(1, 2, 3).GetHashCode());
    }

    [Fact]
    public void Two_hsv_of_the_same_components_are_equal() {
        Check.That(new Hsv(10f, 0.5f, 0.5f)).IsEqualTo(new Hsv(10f, 0.5f, 0.5f));
        Check.That(new Hsv(10f, 0.5f, 0.5f) == new Hsv(10f, 0.5f, 0.5f)).IsTrue();
        Check.That(new Hsv(11f, 0.5f, 0.5f)).IsNotEqualTo(new Hsv(10f, 0.5f, 0.5f));
        Check.That(new Hsv(10f, 0.5f, 0.5f).GetHashCode()).IsEqualTo(new Hsv(10f, 0.5f, 0.5f).GetHashCode());
    }

    [Fact]
    public void Colors_work_as_dictionary_keys() {
        Dictionary<Rgb, string> byColor = new() { { new Rgb(9, 8, 7), "seven" } };

        Check.That(byColor[new Rgb(9, 8, 7)]).IsEqualTo("seven");
    }

    #endregion

    #region invariants

    [Theory]
    [InlineData(-1f, 0.5f, 0.5f)]
    [InlineData(360f, 0.5f, 0.5f)]
    [InlineData(1000f, 0.5f, 0.5f)]
    [InlineData(10f, -0.1f, 0.5f)]
    [InlineData(10f, 1.1f, 0.5f)]
    [InlineData(10f, 0.5f, -0.1f)]
    [InlineData(10f, 0.5f, 1.1f)]
    public void Hsv_refuses_components_out_of_range(float hue, float saturation, float value) {
        Check.ThatCode(() => new Hsv(hue, saturation, value)).Throws<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Hsv_accepts_the_bounds_it_documents() {
        _ = new Hsv(0f, 0f, 0f);
        _ = new Hsv(359.999f, 1f, 1f);
    }

    /// <summary>
    ///     Being a struct, <see cref="Hsv" /> can be obtained without its constructor. The value that comes
    ///     out must still satisfy the invariants, otherwise the type would not guarantee them at all.
    /// </summary>
    [Fact]
    public void The_default_hsv_is_a_legal_color() {
        Hsv uninitialized = default;

        Check.That(uninitialized.Hue).IsGreaterOrEqualThan(0f).And.IsLessOrEqualThan(359.999f);
        Check.That(uninitialized.Saturation).IsGreaterOrEqualThan(0f).And.IsLessOrEqualThan(1f);
        Check.That(uninitialized.Value).IsGreaterOrEqualThan(0f).And.IsLessOrEqualThan(1f);
        Check.That(uninitialized.ToRgb()).IsEqualTo(new Rgb(0, 0, 0));
    }

    [Fact]
    public void The_default_rgb_is_black() {
        Check.That(default(Rgb).ToString()).IsEqualTo("#000000");
    }

    #endregion

    #region conversions

    [Theory]
    [MemberData(nameof(Palette))]
    public void A_color_survives_the_trip_through_BrightColor(byte red, byte green, byte blue) {
        Rgb rgb = BrightColor.From(red, green, blue).ToRgb();

        Check.That(rgb).IsEqualTo(new Rgb(red, green, blue));
    }

    [Theory]
    [MemberData(nameof(Palette))]
    public void A_color_survives_the_trip_through_Hsv(byte red, byte green, byte blue) {
        BrightColor original = BrightColor.From(red, green, blue);

        Rgb roundTripped = ColorService.GetBrightFromHsv(original.ToHsv()).ToRgb();

        // HSV stores hue in degrees and saturation/value as floats, so a byte can come back off by one.
        Check.That(Math.Abs(roundTripped.Red   - red)).IsGreaterOrEqualThan(0).And.IsLessOrEqualThan(1);
        Check.That(Math.Abs(roundTripped.Green - green)).IsGreaterOrEqualThan(0).And.IsLessOrEqualThan(1);
        Check.That(Math.Abs(roundTripped.Blue  - blue)).IsGreaterOrEqualThan(0).And.IsLessOrEqualThan(1);
    }

    [Fact]
    public void Grey_and_black_have_no_saturation() {
        Check.That(BrightColor.From(128, 128, 128).ToHsv().Saturation).IsEqualTo(0f);
        Check.That(BrightColor.Black.ToHsv().Saturation).IsEqualTo(0f);
        Check.That(BrightColor.White.ToHsv().Saturation).IsEqualTo(0f);
    }

    [Fact]
    public void White_is_full_brightness_and_black_none() {
        Check.That(BrightColor.White.ToHsv().Value).IsEqualTo(1f);
        Check.That(BrightColor.Black.ToHsv().Value).IsEqualTo(0f);
    }

    #endregion

    #region interpolation

    [Fact]
    public void Interpolating_to_zero_or_one_lands_on_the_endpoints() {
        Check.That(BrightColor.Red.LerpTo(BrightColor.Blue, 0f)).IsEqualTo(BrightColor.Red);
        Check.That(BrightColor.Red.LerpTo(BrightColor.Blue, 1f)).IsEqualTo(BrightColor.Blue);
    }

    [Fact]
    public void Interpolation_clamps_instead_of_overshooting() {
        Check.That(BrightColor.Red.LerpTo(BrightColor.Blue, -5f)).IsEqualTo(BrightColor.Red);
        Check.That(BrightColor.Red.LerpTo(BrightColor.Blue, 5f)).IsEqualTo(BrightColor.Blue);
    }

    [Fact]
    public void Hue_interpolation_takes_the_short_way_round_the_circle() {
        // 350° to 10° is 20° apart through 0°, not 340° the other way.
        Hsv midway = new Hsv(350f, 1f, 1f).LerpTo(new Hsv(10f, 1f, 1f), 0.5f);

        Check.WithCustomMessage($"expected the hue to pass through 0°, got {midway.Hue}°.").That(midway.Hue is >= 359f or <= 1f).IsTrue();
    }

    /// <summary>
    ///     A hue landing just below zero used to round up to exactly 360 when wrapped, which the constructor
    ///     rejects.
    /// </summary>
    [Fact]
    public void Interpolation_never_produces_a_hue_the_constructor_would_refuse() {
        Hsv from = new(1f, 0.5f, 0.5f);
        Hsv to   = new(200f, 0.5f, 0.5f);

        for (int step = 0; step <= 20000; step++) {
            float t = step / 20000f;
            Hsv   result = from.LerpTo(to, t);

            Check.That(result.Hue).IsGreaterOrEqualThan(0f).And.IsLessOrEqualThan(359.9999f);
        }
    }

    [Fact]
    public void A_pastel_keeps_the_hue_but_drops_the_saturation() {
        BrightColor pastel = BrightColor.Red.GetPastel();

        // Compared to two decimal places, the precision the conversion promises.
        Check.That(MathF.Round(pastel.ToHsv().Saturation, 2)).IsEqualTo(0.5f);
        Check.That(MathF.Round(pastel.ToHsv().Value, 2)).IsEqualTo(0.7f);
    }

    #endregion

}
