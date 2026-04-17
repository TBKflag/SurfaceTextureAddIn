using System;
using System.Windows.Forms;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.UI;

internal sealed class TextureCommandForm : Form
{
    private readonly NumericUpDown spacingUNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.001M, Maximum = 1M, Value = 0.01M };
    private readonly NumericUpDown spacingVNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.001M, Maximum = 1M, Value = 0.01M };
    private readonly NumericUpDown heightNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0.0001M, Maximum = 0.2M, Value = 0.001M };
    private readonly NumericUpDown marginNumeric = new() { DecimalPlaces = 4, Increment = 0.001M, Minimum = 0M, Maximum = 0.2M, Value = 0M };
    private readonly NumericUpDown rotationNumeric = new() { DecimalPlaces = 1, Increment = 1M, Minimum = -360M, Maximum = 360M, Value = 0M };
    private readonly NumericUpDown maxInstancesNumeric = new() { DecimalPlaces = 0, Increment = 10M, Minimum = 1M, Maximum = 5000M, Value = 500M };
    private readonly NumericUpDown maxNormalDeltaNumeric = new() { DecimalPlaces = 1, Increment = 1M, Minimum = 1M, Maximum = 180M, Value = 25M };
    private readonly CheckBox curvatureCheckBox = new() { Text = "Skip high-curvature regions", Checked = true, AutoSize = true };
    private readonly Label selectionHintLabel = new() { AutoSize = true, Text = "Before running, select one texture body and one target face in SolidWorks." };

    public TextureCommandForm(TextureOperationMode mode)
    {
        Text = mode == TextureOperationMode.Boss ? "Generate Convex Texture" : "Generate Concave Texture";
        Width = 420;
        Height = 360;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(12),
            AutoSize = true,
        };

        layout.Controls.Add(selectionHintLabel, 0, 0);
        layout.SetColumnSpan(selectionHintLabel, 2);
        AddField(layout, 1, "Spacing U (m)", spacingUNumeric);
        AddField(layout, 2, "Spacing V (m)", spacingVNumeric);
        AddField(layout, 3, mode == TextureOperationMode.Boss ? "Height (m)" : "Depth (m)", heightNumeric);
        AddField(layout, 4, "Margin (UV units)", marginNumeric);
        AddField(layout, 5, "Rotation (deg)", rotationNumeric);
        AddField(layout, 6, "Max instances", maxInstancesNumeric);
        AddField(layout, 7, "Max normal delta", maxNormalDeltaNumeric);
        layout.Controls.Add(curvatureCheckBox, 0, 8);
        layout.SetColumnSpan(curvatureCheckBox, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12),
            Height = 48,
        };

        var okButton = new Button { Text = "Run", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

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
}
