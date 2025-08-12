#region Usings declarations

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Services;

public static class PredefinedColor {

    #region Statics members declarations

    public static BrightColor Available    = BrightColor.Green;
    public static BrightColor Busy         = BrightColor.Yellow;
    public static BrightColor DoNotDisturb = BrightColor.Red;
    public static BrightColor Away         = BrightColor.From(153, 50, 204);

    #endregion

}