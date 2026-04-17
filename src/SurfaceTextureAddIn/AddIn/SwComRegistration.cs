using System;
using Microsoft.Win32;

namespace SurfaceTextureAddIn.AddIn;

internal static class SwComRegistration
{
    private const string AddInsKeyTemplate = @"SOFTWARE\SolidWorks\AddIns\{{{0}}}";
    private const string AddInsStartupKeyTemplate = @"SOFTWARE\SolidWorks\AddInsStartup\{{{0}}}";

    public static void Register(Type type)
    {
        using var addInKey = Registry.LocalMachine.CreateSubKey(string.Format(AddInsKeyTemplate, type.GUID));
        addInKey?.SetValue(null, 1, RegistryValueKind.DWord);
        addInKey?.SetValue("Title", "Surface Texture Add-In");
        addInKey?.SetValue("Description", "Generates convex and concave textures on selected SolidWorks faces.");

        using var startupKey = Registry.CurrentUser.CreateSubKey(string.Format(AddInsStartupKeyTemplate, type.GUID));
        startupKey?.SetValue(null, 1, RegistryValueKind.DWord);
    }

    public static void Unregister(Type type)
    {
        Registry.LocalMachine.DeleteSubKeyTree(string.Format(AddInsKeyTemplate, type.GUID), false);
        Registry.CurrentUser.DeleteSubKeyTree(string.Format(AddInsStartupKeyTemplate, type.GUID), false);
    }
}
