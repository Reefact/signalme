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
        Assert.Equal(new Rgb(1, 2, 3), new Rgb(1, 2, 3));
        Assert.True(new Rgb(1, 2, 3) == new Rgb(1, 2, 3));
        Assert.False(new Rgb(1, 2, 3) != new Rgb(1, 2, 3));
        Assert.NotEqual(new Rgb(1, 2, 3), new Rgb(3, 2, 1));
        Assert.Equal(new Rgb(1, 2, 3).GetHashCode(), new Rgb(1, 2, 3).GetHashCode());
    }

    [Fact]
    public void Two_hsv_of_the_same_components_are_equal() {
        Assert.Equal(new Hsv(10f, 0.5f, 0.5f), new Hsv(10f, 0.5f, 0.5f));
        Assert.True(new Hsv(10f, 0.5f, 0.5f) == new Hsv(10f, 0.5f, 0.5f));
        Assert.NotEqual(new Hsv(10f, 0.5f, 0.5f), new Hsv(11f, 0.5f, 0.5f));
        Assert.Equal(new Hsv(10f, 0.5f, 0.5f).GetHashCode(), new Hsv(10f, 0.5f, 0.5f).GetHashCode());
    }

    [Fact]
    public void Colors_work_as_dictionary_keys() {
        Dictionary<Rgb, string> byColor = new() { { new Rgb(9, 8, 7), "seven" } };

        Assert.Equal("seven", byColor[new Rgb(9, 8, 7)]);
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
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hsv(hue, saturation, value));
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

        Assert.InRange(uninitialized.Hue, 0f, 359.999f);
        Assert.InRange(uninitialized.Saturation, 0f, 1f);
        Assert.InRange(uninitialized.Value, 0f, 1f);
        Assert.Equal(new Rgb(0, 0, 0), uninitialized.ToRgb());
    }

    [Fact]
    public void The_default_rgb_is_black() {
        Assert.Equal("#000000", default(Rgb).ToString());
    }

    #endregion

    #region conversions

    [Theory]
    [MemberData(nameof(Palette))]
    public void A_color_survives_the_trip_through_BrightColor(byte red, byte green, byte blue) {
        Rgb rgb = BrightColor.From(red, green, blue).ToRgb();

        Assert.Equal(new Rgb(red, green, blue), rgb);
    }

    [Theory]
    [MemberData(nameof(Palette))]
    public void A_color_survives_the_trip_through_Hsv(byte red, byte green, byte blue) {
        BrightColor original = BrightColor.From(red, green, blue);

        Rgb roundTripped = ColorService.GetBrightFromHsv(original.ToHsv()).ToRgb();

        // HSV stores hue in degrees and saturation/value as floats, so a byte can come back off by one.
        Assert.InRange(Math.Abs(roundTripped.Red   - red), 0, 1);
        Assert.InRange(Math.Abs(roundTripped.Green - green), 0, 1);
        Assert.InRange(Math.Abs(roundTripped.Blue  - blue), 0, 1);
    }

    [Fact]
    public void Grey_and_black_have_no_saturation() {
        Assert.Equal(0f, BrightColor.From(128, 128, 128).ToHsv().Saturation);
        Assert.Equal(0f, BrightColor.Black.ToHsv().Saturation);
        Assert.Equal(0f, BrightColor.White.ToHsv().Saturation);
    }

    [Fact]
    public void White_is_full_brightness_and_black_none() {
        Assert.Equal(1f, BrightColor.White.ToHsv().Value);
        Assert.Equal(0f, BrightColor.Black.ToHsv().Value);
    }

    #endregion

    #region interpolation

    [Fact]
    public void Interpolating_to_zero_or_one_lands_on_the_endpoints() {
        Assert.Equal(BrightColor.Red, BrightColor.Red.LerpTo(BrightColor.Blue, 0f));
        Assert.Equal(BrightColor.Blue, BrightColor.Red.LerpTo(BrightColor.Blue, 1f));
    }

    [Fact]
    public void Interpolation_clamps_instead_of_overshooting() {
        Assert.Equal(BrightColor.Red, BrightColor.Red.LerpTo(BrightColor.Blue, -5f));
        Assert.Equal(BrightColor.Blue, BrightColor.Red.LerpTo(BrightColor.Blue, 5f));
    }

    [Fact]
    public void Hue_interpolation_takes_the_short_way_round_the_circle() {
        // 350° to 10° is 20° apart through 0°, not 340° the other way.
        Hsv midway = new Hsv(350f, 1f, 1f).LerpTo(new Hsv(10f, 1f, 1f), 0.5f);

        Assert.True(midway.Hue is >= 359f or <= 1f, $"expected the hue to pass through 0°, got {midway.Hue}°.");
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

            Assert.InRange(result.Hue, 0f, 359.9999f);
        }
    }

    [Fact]
    public void A_pastel_keeps_the_hue_but_drops_the_saturation() {
        BrightColor pastel = BrightColor.Red.GetPastel();

        Assert.Equal(0.5f, pastel.ToHsv().Saturation, 2);
        Assert.Equal(0.7f, pastel.ToHsv().Value, 2);
    }

    #endregion

}
