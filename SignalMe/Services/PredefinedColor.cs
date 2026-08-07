#region Usings declarations

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Services;

public static class PredefinedColor {

    #region Statics members declarations

    public static readonly BrightColor Available    = BrightColor.Green;
    public static readonly BrightColor Busy         = BrightColor.Yellow;
    public static readonly BrightColor DoNotDisturb = BrightColor.Red;
    public static readonly BrightColor Away         = BrightColor.From(153, 50, 204);

    #endregion

}