using System;
using System.Windows.Forms;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.UI;

internal sealed class TextureCommandForm : Form
{
    private readonly dynamic swApp;
    private readonly Label seedSelectionLabel = new() { AutoSize = true, Text = "Not selected" };
    private readonly Label faceSelectionLabel = new() { AutoSize = true, Text = "Not selected" };
    private readonly NumericUpDown spacingUNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.001M, Maximum = 1M, Value = 0.01M };
    private readonly NumericUpDown spacingVNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.001M, Maximum = 1M, Value = 0.01M };
    private readonly NumericUpDown heightNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.0001M, Maximum = 0.2M, Value = 0.001M };
    private readonly NumericUpDown marginNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0M, Maximum = 0.2M, Value = 0M };
    private readonly NumericUpDown rotationNumeric = new() { DecimalPlaces = 1, Increment = 1M, Minimum = -360M, Maximum = 360M, Value = 0M };
    private readonly NumericUpDown maxInstancesNumeric = new() { DecimalPlaces = 0, Increment = 10M, Minimum = 1M, Maximum = 5000M, Value = 500M };
    private readonly NumericUpDown maxNormalDeltaNumeric = new() { DecimalPlaces = 1, Increment = 1M, Minimum = 1M, Maximum = 180M, Value = 25M };
    private readonly CheckBox curvatureCheckBox = new() { Text = "Skip high-curvature regions", Checked = true, AutoSize = true };
    private readonly Label selectionHintLabel = new() { AutoSize = true, Text = "Select one texture body and one target face in SolidWorks, then click the capture buttons." };
    private object? selectedSeedBody;
    private object? selectedTargetFace;
    private bool submitted;

    public TextureCommandForm(dynamic swApp, TextureOperationMode mode)
    {
        this.swApp = swApp;
        Text = mode == TextureOperationMode.Boss ? "Generate Convex Texture" : "Generate Concave Texture";
        Width = 560;
        Height = 430;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 13,
            Padding = new Padding(12),
            AutoSize = true,
        };

        layout.Controls.Add(selectionHintLabel, 0, 0);
        layout.SetColumnSpan(selectionHintLabel, 2);
        AddSelectionField(layout, 1, "Texture Seed Body", seedSelectionLabel, CaptureSeedBody);
        AddSelectionField(layout, 2, "Target Face", faceSelectionLabel, CaptureTargetFace);
        AddField(layout, 4, "Spacing U (m)", spacingUNumeric);
        AddField(layout, 5, "Spacing V (m)", spacingVNumeric);
        AddField(layout, 6, mode == TextureOperationMode.Boss ? "Height (m)" : "Depth (m)", heightNumeric);
        AddField(layout, 7, "Margin (UV units)", marginNumeric);
        AddField(layout, 8, "Rotation (deg)", rotationNumeric);
        AddField(layout, 9, "Max instances", maxInstancesNumeric);
        AddField(layout, 10, "Max normal delta", maxNormalDeltaNumeric);
        layout.Controls.Add(curvatureCheckBox, 0, 11);
        layout.SetColumnSpan(curvatureCheckBox, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12),
            Height = 48,
        };

        var okButton = new Button { Text = "Run", Width = 90 };
        var cancelButton = new Button { Text = "Cancel", Width = 90 };
        okButton.Click += (_, _) =>
        {
            if (selectedSeedBody is null || selectedTargetFace is null)
            {
                MessageBox.Show(
                    "Please capture both selections before running.\n- Texture Seed Body\n- Target Face",
                    "Surface Texture Add-In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            submitted = true;
            Close();
        };
        cancelButton.Click += (_, _) =>
        {
            submitted = false;
            Close();
        };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public bool Submitted => submitted;
    public object? SelectedSeedBody => selectedSeedBody;
    public object? SelectedTargetFace => selectedTargetFace;

    public TextureParameters GetParameters()
    {
        return new TextureParameters
        {
            SpacingU = (double)spacingUNumeric.Value,
            SpacingV = (double)spacingVNumeric.Value,
            HeightOrDepth = (double)heightNumeric.Value,
            Margin = (double)marginNumeric.Value,
            RotationDegrees = (double)rotationNumeric.Value,
            MaxInstances = (int)maxInstancesNumeric.Value,
            SkipHighCurvature = curvatureCheckBox.Checked,
            MaxNormalDeltaDegrees = (double)maxNormalDeltaNumeric.Value,
        };
    }

    private static void AddField(TableLayoutPanel layout, int row, string labelText, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) };
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddSelectionField(TableLayoutPanel layout, int row, string labelText, Label valueLabel, Action captureAction)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) };
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
        };
        var button = new Button { Text = "Capture Current Selection", AutoSize = true };
        button.Click += (_, _) => captureAction();
        valueLabel.Margin = new Padding(8, 8, 3, 3);
        panel.Controls.Add(button);
        panel.Controls.Add(valueLabel);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(panel, 1, row);
    }

    private void CaptureSeedBody()
    {
        var selected = TryGetSelectedObject(72);
        if (selected is null)
        {
            MessageBox.Show("No solid body selected in SolidWorks.", "Surface Texture Add-In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        selectedSeedBody = selected;
        seedSelectionLabel.Text = "Captured";
    }

    private void CaptureTargetFace()
    {
        var selected = TryGetSelectedObject(2);
        if (selected is null)
        {
            MessageBox.Show("No face selected in SolidWorks.", "Surface Texture Add-In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        selectedTargetFace = selected;
        faceSelectionLabel.Text = "Captured";
    }

    private object? TryGetSelectedObject(int expectedType)
    {
        try
        {
            object? activeDocObject = swApp?.ActiveDoc;
            if (activeDocObject is null)
            {
                return null;
            }

            dynamic activeDoc = activeDocObject;
            object? selectionManagerObject = activeDoc.SelectionManager;
            if (selectionManagerObject is null)
            {
                return null;
            }

            dynamic selectionManager = selectionManagerObject;
            int count = selectionManager.GetSelectedObjectCount2(-1);
            for (var index = 1; index <= count; index++)
            {
                int type = selectionManager.GetSelectedObjectType3(index, -1);
                if (type == expectedType)
                {
                    return selectionManager.GetSelectedObject6(index, -1);
                }
            }
        }
        catch
        {
            // Keep null result for the caller.
        }

        return null;
    }
}
