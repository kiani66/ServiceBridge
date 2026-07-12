using MudBlazor;

namespace ServiceBridge.Web.Theme;

public static class ServiceBridgeTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2563EB",
            Secondary = "#0F6CBD",
            Tertiary = "#0891B2",
            Success = "#15803D",
            Warning = "#B45309",
            Error = "#B91C1C",
            Info = "#2563EB",
            Background = "#F5F7FB",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#172033",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#344054",
            TextPrimary = "#172033",
            TextSecondary = "#667085",
            LinesDefault = "#E4E7EC"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Segoe UI", "system-ui", "sans-serif"]
            }
        }
    };
}
