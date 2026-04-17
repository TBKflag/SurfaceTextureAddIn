namespace SurfaceTextureAddIn.Core;

internal static class SwApiConstants
{
    public const int Ok = 0;

    public const int DocumentPart = 1;

    public const int MessageBoxStop = 0;
    public const int MessageBoxWarning = 1;
    public const int MessageBoxInformation = 2;

    public const int BodyOperationAdd = 0;
    public const int BodyOperationCut = 1;
    public const int BodyOperationIntersect = 2;

    public const int SelectionTypeFaces = 2;
    public const int SelectionTypeBodies = 72;

    public const int CommandItemMenu = 1;
    public const int CommandItemToolbar = 2;
    public const int CommandItemMenuAndToolbar = CommandItemMenu | CommandItemToolbar;
}
