using System;

namespace SurfaceTextureAddIn.Geometry;

internal readonly struct Vector3D
{
    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public Vector3D Normalize()
    {
        var length = Length;
        return length <= 1e-9 ? new Vector3D(0, 0, 0) : new Vector3D(X / length, Y / length, Z / length);
    }

    public double Dot(Vector3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    public Vector3D Cross(Vector3D other)
    {
        return new Vector3D(
            (Y * other.Z) - (Z * other.Y),
            (Z * other.X) - (X * other.Z),
            (X * other.Y) - (Y * other.X));
    }

    public Vector3D RotateAround(Vector3D axis, double radians)
    {
        var unitAxis = axis.Normalize();
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return (this * cos) + (unitAxis.Cross(this) * sin) + (unitAxis * (unitAxis.Dot(this) * (1 - cos)));
    }

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D value, double scale) => new(value.X * scale, value.Y * scale, value.Z * scale);

    public double[] ToArray() => new[] { X, Y, Z };

    public static Vector3D FromArray(double[] values)
    {
        if (values.Length < 3)
        {
            throw new ArgumentException("Expected at least three coordinates.", nameof(values));
        }

        return new Vector3D(values[0], values[1], values[2]);
    }
}
