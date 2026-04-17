using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Commands;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.AddIn;

[ComVisible(true)]
[Guid("2EB261C8-FD72-4AE6-AF0A-A3E6D755984A")]
[ProgId("SurfaceTextureAddIn.Connect")]
public sealed class SurfaceTextureAddIn : ISwAddin
{
    private const int MainCommandGroupId = 0x5B17;
    private const int ConvexCommandId = 0;
    private const int ConcaveCommandId = 1;

    private SldWorks? swApp;
    private dynamic? commandManager;
    private int addInCookie;
    private TextureCommandController? commandController;

    public bool ConnectToSW(object ThisSW, int Cookie)
    {
        swApp = (SldWorks)ThisSW;
        addInCookie = Cookie;
        commandController = new TextureCommandController(swApp);

        swApp.SetAddinCallbackInfo2(0, this, addInCookie);
        commandManager = swApp.GetCommandManager(addInCookie);
        RegisterCommands();
        return true;
    }

    public bool DisconnectFromSW()
    {
        UnregisterCommands();
        commandController = null;
        commandManager = null;
        swApp = null;
        return true;
    }

    public void OnGenerateConvexTexture()
    {
        commandController?.Run(TextureOperationMode.Boss);
    }

    public void OnGenerateConcaveTexture()
    {
        commandController?.Run(TextureOperationMode.Cut);
    }

    public int CanExecuteTextureCommand()
    {
        return swApp?.ActiveDoc is null ? 0 : 1;
    }

    [ComRegisterFunction]
    public static void RegisterFunction(Type type)
    {
        SwComRegistration.Register(type);
    }

    [ComUnregisterFunction]
    public static void UnregisterFunction(Type type)
    {
        SwComRegistration.Unregister(type);
    }

    private void RegisterCommands()
    {
        if (commandManager is null)
        {
            return;
        }

        int errors = 0;
        var commandGroup = commandManager.CreateCommandGroup2(
            MainCommandGroupId,
            "Surface Texture",
            "Generate convex and concave textures on a face",
            "Surface Texture",
            -1,
            true,
            ref errors);

        commandGroup.AddCommandItem2(
            "Generate Convex Texture",
            -1,
            "Distribute texture seed bodies over the selected face and add them to the target body.",
            "Convex Texture",
            ConvexCommandId,
            nameof(OnGenerateConvexTexture),
            nameof(CanExecuteTextureCommand),
            SwApiConstants.CommandItemMenuAndToolbar);

        commandGroup.AddCommandItem2(
            "Generate Concave Texture",
            -1,
            "Distribute texture seed bodies over the selected face and subtract them from the target body.",
            "Concave Texture",
            ConcaveCommandId,
            nameof(OnGenerateConcaveTexture),
            nameof(CanExecuteTextureCommand),
            SwApiConstants.CommandItemMenuAndToolbar);

        commandGroup.HasToolbar = true;
        commandGroup.HasMenu = true;
        commandGroup.Activate();

    }

    private void UnregisterCommands()
    {
        if (commandManager is null)
        {
            return;
        }

        commandManager.RemoveCommandGroup2(MainCommandGroupId, true);
    }
}
