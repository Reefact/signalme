#region Usings declarations

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Services;

public static class ColorService {

    #region Statics members declarations

    public static BrightColor GetBrightFromHsv(Hsv hsv) {
        Rgb rgb = hsv.ToRgb();

        return BrightColor.From(rgb.Red, rgb.Green, rgb.Blue);
    }

    #endregion

}