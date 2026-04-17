using System;
using SurfaceTextureAddIn.Geometry;

namespace SurfaceTextureAddIn.Models;

internal enum TextureOperationMode
{
    Boss,
    Cut
}

internal sealed class TextureParameters
{
    public double SpacingU { get; set; } = 0.01;
    public double SpacingV { get; set; } = 0.01;
    public double HeightOrDepth { get; set; } = 0.001;
    public double Margin { get; set; } = 0.0;
    public double RotationDegrees { get; set; } = 0.0;
    public int MaxInstances { get; set; } = 500;
    public bool SkipHighCurvature { get; set; } = true;
    public double MaxNormalDeltaDegrees { get; set; } = 25.0;

    public double RotationRadians => RotationDegrees * (Math.PI / 180.0);
}

internal sealed class TexturePageResult
{
    public TextureParameters Parameters { get; set; } = new();
    public object? SelectedSeedBody { get; set; }
    public object? SelectedTargetFace { get; set; }
}

internal sealed class TextureSeedDefinition
{
    public object? Body { get; set; }
    public object? BaseFace { get; set; }
    public Vector3D BaseOrigin { get; set; }
    public Vector3D BaseNormal { get; set; } = new(0, 0, 1);
    public Vector3D BaseTangent { get; set; } = new(1, 0, 0);
    public Vector3D BaseBitangent { get; set; } = new(0, 1, 0);
    public double Width { get; set; }
    public double Height { get; set; }
    public double Thickness { get; set; }
}

internal sealed class FaceSample
{
    public double U { get; set; }
    public double V { get; set; }
    public Vector3D Position { get; set; } = new(0, 0, 0);
    public Vector3D Normal { get; set; } = new(0, 0, 1);
    public Vector3D TangentU { get; set; } = new(1, 0, 0);
    public Vector3D TangentV { get; set; } = new(0, 1, 0);
    public bool IsValid { get; set; }
}

internal sealed class PlacementFrame
{
    public int Index { get; set; }
    public FaceSample Sample { get; set; } = new();
    public Vector3D XAxis { get; set; } = new(1, 0, 0);
    public Vector3D YAxis { get; set; } = new(0, 1, 0);
    public Vector3D ZAxis { get; set; } = new(0, 0, 1);
    public bool Collides { get; set; }
}

internal sealed class TextureExecutionContext
{
    public object? ActiveDocument { get; set; }
    public object? TargetFace { get; set; }
    public object? TargetBody { get; set; }
    public object? SelectedSeedBody { get; set; }
    public TextureSeedDefinition? Seed { get; set; }
    public TextureParameters Parameters { get; set; } = new();
    public TextureOperationMode Mode { get; set; }
}
