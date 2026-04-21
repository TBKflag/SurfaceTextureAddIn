using System;
using System.Threading;
using System.Windows.Forms;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.UI;

internal sealed class TexturePropertyManagerPage : IPropertyManagerPage2Handler9
{
    private const int GroupId = 1;
    private const int SeedSelectionId = 10;
    private const int FaceSelectionId = 11;
    private const int SpacingUId = 20;
    private const int SpacingVId = 21;
    private const int HeightDepthId = 22;
    private const int MarginId = 23;
    private const int RotationId = 24;
    private const int MaxInstancesId = 25;
    private const int SkipCurvatureId = 26;
    private const int MaxNormalDeltaId = 27;
    private const int InstructionsId = 28;

    private dynamic? swApp;
    private dynamic? page;
    private TextureOperationMode mode;
    private TexturePageResult? result;
    private readonly ManualResetEventSlim closeSignal = new(false);

    private dynamic? seedSelectionBox;
    private dynamic? faceSelectionBox;
    private dynamic? spacingUBox;
    private dynamic? spacingVBox;
    private dynamic? heightDepthBox;
    private dynamic? marginBox;
    private dynamic? rotationBox;
    private dynamic? maxInstancesBox;
    private dynamic? maxNormalDeltaBox;
    private dynamic? skipCurvatureCheckBox;

    public TexturePageResult? Show(dynamic solidWorksApplication, TextureOperationMode requestedMode)
    {
        swApp = solidWorksApplication;
        mode = requestedMode;
        result = null;
        closeSignal.Reset();

        try
        {
            if (TryShowNativePage())
            {
                while (!closeSignal.IsSet)
                {
                    Application.DoEvents();
                    Thread.Sleep(15);
                }

                return result;
            }
        }
        catch
        {
            // Fall back to the simple form below.
        }

        using var form = new TextureCommandForm(swApp, mode);
        form.Show();
        while (form.Visible)
        {
            Application.DoEvents();
            Thread.Sleep(15);
        }

        return form.Submitted
            ? new TexturePageResult
            {
                Parameters = form.GetParameters(),
                SelectedSeedBody = form.SelectedSeedBody,
                SelectedTargetFace = form.SelectedTargetFace
            }
            : null;
    }

    private bool TryShowNativePage()
    {
        if (swApp is null)
        {
            return false;
        }

        int errors = 0;
        page = swApp.CreatePropertyManagerPage(
            mode == TextureOperationMode.Boss ? "Generate Convex Texture" : "Generate Concave Texture",
            (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_OkayButton +
            (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_CancelButton,
            this,
            ref errors);

        if (page is null || errors != 0)
        {
            return false;
        }

        CreateControls();
        page.Show2(0);
        return true;
    }

    private void CreateControls()
    {
        if (page is null)
        {
            return;
        }

        dynamic group = page.AddGroupBox(
            GroupId,
            "Texture Parameters",
            (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Expanded |
            (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Visible);

        group.Visible = true;

        group.AddControl2(
            InstructionsId,
            (short)swPropertyManagerPageControlType_e.swControlType_Label,
            "Select one seed body and one target face, then adjust the values below.",
            (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge,
            0,
            "The page stores the selected body and face directly.");

        seedSelectionBox = group.AddControl2(
            SeedSelectionId,
            (short)swPropertyManagerPageControlType_e.swControlType_Selectionbox,
            "Texture Seed Body",
            (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge,
            (int)swAddControlOptions_e.swControlOptions_Visible |
            (int)swAddControlOptions_e.swControlOptions_Enabled,
            "Select a single solid body used as the texture seed.");
        seedSelectionBox.SingleEntityOnly = true;
        seedSelectionBox.AllowMultipleSelectOfSameEntity = false;
        seedSelectionBox.SetSelectionFilters(new[] { (int)swSelectType_e.swSelSOLIDBODIES });

        faceSelectionBox = group.AddControl2(
            FaceSelectionId,
            (short)swPropertyManagerPageControlType_e.swControlType_Selectionbox,
            "Target Face",
            (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge,
            (int)swAddControlOptions_e.swControlOptions_Visible |
            (int)swAddControlOptions_e.swControlOptions_Enabled,
            "Select a single face that will receive the texture.");
        faceSelectionBox.SingleEntityOnly = true;
        faceSelectionBox.AllowMultipleSelectOfSameEntity = false;
        faceSelectionBox.SetSelectionFilters(new[] { (int)swSelectType_e.swSelFACES });

        spacingUBox = CreateNumberBox(group, SpacingUId, "Spacing U (m)", 0.01, 0.001, 1.0, 0.001);
        spacingVBox = CreateNumberBox(group, SpacingVId, "Spacing V (m)", 0.01, 0.001, 1.0, 0.001);
        heightDepthBox = CreateNumberBox(group, HeightDepthId, mode == TextureOperationMode.Boss ? "Height (m)" : "Depth (m)", 0.001, 0.0001, 0.2, 0.001);
        marginBox = CreateNumberBox(group, MarginId, "Margin", 0.0, 0.0, 0.2, 0.001);
        rotationBox = CreateNumberBox(group, RotationId, "Rotation (deg)", 0.0, -360.0, 360.0, 1.0);
        maxInstancesBox = CreateNumberBox(group, MaxInstancesId, "Max Instances", 500.0, 1.0, 5000.0, 10.0);
        maxNormalDeltaBox = CreateNumberBox(group, MaxNormalDeltaId, "Max Normal Delta", 25.0, 1.0, 180.0, 1.0);

        skipCurvatureCheckBox = group.AddControl2(
            SkipCurvatureId,
            (short)swPropertyManagerPageControlType_e.swControlType_Checkbox,
            "Skip high-curvature regions",
            (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge,
            (int)swAddControlOptions_e.swControlOptions_Visible |
            (int)swAddControlOptions_e.swControlOptions_Enabled,
            "Skip placements that change surface normal too abruptly.");
        skipCurvatureCheckBox.Checked = true;
    }

    private static dynamic CreateNumberBox(dynamic group, int id, string label, double value, double min, double max, double increment)
    {
        dynamic numberBox = group.AddControl2(
            id,
            (short)swPropertyManagerPageControlType_e.swControlType_Numberbox,
            label,
            (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge,
            (int)swAddControlOptions_e.swControlOptions_Visible |
            (int)swAddControlOptions_e.swControlOptions_Enabled,
            label);
        numberBox.SetRange2((int)swNumberboxUnitType_e.swNumberBox_UnitlessDouble, min, max, true, increment, increment, increment);
        numberBox.Value = value;
        return numberBox;
    }

    private void CaptureSelectionsFromActiveDocument()
    {
        if (swApp?.ActiveDoc is null)
        {
            return;
        }

        dynamic activeDoc = swApp.ActiveDoc;
        dynamic selectionManager = activeDoc.SelectionManager;
        int count = selectionManager.GetSelectedObjectCount2(-1);
        object? seedBody = null;
        object? targetFace = null;

        for (var index = 1; index <= count; index++)
        {
            object candidate;
            int type;
            try
            {
                candidate = selectionManager.GetSelectedObject6(index, -1);
                type = selectionManager.GetSelectedObjectType3(index, -1);
            }
            catch
            {
                continue;
            }

            if (type == (int)swSelectType_e.swSelSOLIDBODIES && seedBody is null)
            {
                seedBody = candidate;
            }
            else if (type == (int)swSelectType_e.swSelFACES && targetFace is null)
            {
                targetFace = candidate;
            }
        }

        result ??= new TexturePageResult();
        result.SelectedSeedBody = seedBody;
        result.SelectedTargetFace = targetFace;
    }

    private TextureParameters ReadParameters()
    {
        return new TextureParameters
        {
            SpacingU = (double)spacingUBox?.Value,
            SpacingV = (double)spacingVBox?.Value,
            HeightOrDepth = (double)heightDepthBox?.Value,
            Margin = (double)marginBox?.Value,
            RotationDegrees = (double)rotationBox?.Value,
            MaxInstances = Convert.ToInt32((double)maxInstancesBox?.Value),
            SkipHighCurvature = (bool)skipCurvatureCheckBox?.Checked,
            MaxNormalDeltaDegrees = (double)maxNormalDeltaBox?.Value,
        };
    }

    public void AfterActivation()
    {
    }

    public void OnClose(int reason)
    {
        if (reason == (int)swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_Okay)
        {
            result ??= new TexturePageResult();
            result.Parameters = ReadParameters();
            CaptureSelectionsFromActiveDocument();
        }
        else
        {
            result = null;
        }

        closeSignal.Set();
    }

    public bool OnSubmitSelection(int id, object selection, int selType, ref string itemText) => true;
    public void OnSelectionboxListChanged(int id, int count) => CaptureSelectionsFromActiveDocument();
    public void OnNumberBoxChanged(int id, double value) { }
    public void OnCheckboxCheck(int id, bool isChecked) { }
    public void OnButtonPress(int id) { }
    public int OnActiveXControlCreated(int id, bool status) => 0;
    public void OnComboboxEditChanged(int id, string text) { }
    public int OnComboboxSelectionChanged(int id, int item) => 0;
    public void OnGroupCheck(int id, bool checkedState) { }
    public void OnGroupExpand(int id, bool expanded) { }
    public bool OnHelp() => true;
    public bool OnKeystroke(int wParam, int message, int lParam, int id) => true;
    public void OnListboxSelectionChanged(int id, int item) { }
    public bool OnNextPage() => true;
    public void OnOptionCheck(int id) { }
    public void OnPopupMenuItem(int id) { }
    public void OnPopupMenuItemUpdate(int id, ref int retval) { }
    public bool OnPreview() => true;
    public void OnRedo() { }
    public bool OnSelectionboxCalloutCreated(int id) => true;
    public void OnSelectionboxCalloutDestroyed(int id) { }
    public void OnSelectionboxFocusChanged(int id) { }
    public void OnSliderPositionChanged(int id, double value) { }
    public void OnSliderTrackingCompleted(int id, double value) { }
    public void OnTextboxChanged(int id, string text) { }
    public void OnUndo() { }
    public void OnWhatsNew() { }
    public bool OnGainedFocus(int id) => true;
    public bool OnLostFocus(int id) => true;
    public int OnWindowFromHandleControlCreated(int id, bool status) => 0;
    public void OnNumberboxTrackingCompleted(int id, double value) { }
    public bool OnSelectionboxListDoubleClicked(int id, int item) => true;
    public int OnSelectionboxSelectionChanged(int id, int count) => 0;
    public void AfterClose() { }

    public bool OnPreviousPage()
    {
        return true;
    }

    public bool OnTabClicked(int Id)
    {
        return true;
    }

    public void OnNumberboxChanged(int Id, double Value)
    {
        // No-op.
    }

    void IPropertyManagerPage2Handler9.OnComboboxSelectionChanged(int Id, int Item)
    {
        // No-op.
    }

    void IPropertyManagerPage2Handler9.OnSelectionboxCalloutCreated(int Id)
    {
        // No-op.
    }

    void IPropertyManagerPage2Handler9.OnGainedFocus(int Id)
    {
        // No-op.
    }

    void IPropertyManagerPage2Handler9.OnLostFocus(int Id)
    {
        // No-op.
    }

    public void OnListboxRMBUp(int Id, int PosX, int PosY)
    {
        // No-op.
    }

    public void OnNumberBoxTrackingCompleted(int Id, double Value)
    {
        // No-op.
    }
}
