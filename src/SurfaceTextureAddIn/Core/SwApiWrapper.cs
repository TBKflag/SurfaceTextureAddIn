using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Geometry;

namespace SurfaceTextureAddIn.Core;

internal static class SwApiWrapper
{
    #region IBody2 API

    /// <summary>
    /// Copies the body.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~Copy2.html</para>
    /// </summary>
    public static IBody2? CopyBody(IBody2 body, bool preserveFaceIds = false)
    {
        if (body == null) return null;
        try
        {
            return body.Copy2(preserveFaceIds) as IBody2;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Performs a boolean operation on the body.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~Operations2.html</para>
    /// </summary>
    public static IBody2? PerformBooleanOperation(IBody2 targetBody, IBody2 toolBody, int operationType, out int errorCode)
    {
        errorCode = -1;
        if (targetBody == null || toolBody == null) return null;
        
        try
        {
            var result = targetBody.Operations2(operationType, toolBody, out errorCode);
            if (result is object[] resultBodies && resultBodies.Length > 0)
            {
                return resultBodies[0] as IBody2;
            }
        }
        catch (Exception)
        {
        }
        
        return null;
    }

    /// <summary>
    /// Applies a transform to the body.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~ApplyTransform.html</para>
    /// </summary>
    public static bool ApplyTransformToBody(IBody2 body, MathTransform transform)
    {
        if (body == null || transform == null) return false;
        try
        {
            body.ApplyTransform(transform);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region IFace2 API

    /// <summary>
    /// Gets the UV bounds of the face.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFace2~GetUVBounds.html</para>
    /// </summary>
    public static double[]? GetFaceUVBounds(object face)
    {
        if (face == null) return null;
        try
        {
            dynamic f = face;
            return f.GetUVBounds() as double[];
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the surface of the face.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFace2~GetSurface.html</para>
    /// </summary>
    public static object? GetFaceSurface(object face)
    {
        if (face == null) return null;
        try
        {
            dynamic f = face;
            return f.GetSurface();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Evaluates the face at the specified UV coordinates.
    /// Uses GetUVBounds() and ISurface.Evaluate() as documented.
    /// </summary>
    public static bool EvaluateFaceAtCenter(object face, out Vector3D position, out Vector3D normal)
    {
        position = new Vector3D();
        normal = new Vector3D();

        var uv = GetFaceUVBounds(face);
        if (uv == null || uv.Length < 4)
            return false;

        var surface = GetFaceSurface(face);
        if (surface == null)
            return false;

        return EvaluateSurface(surface, (uv[0] + uv[1]) / 2, (uv[2] + uv[3]) / 2, out position, out normal);
    }

    #endregion

    #region ISurface API

    /// <summary>
    /// Evaluates the surface at the specified UV coordinates.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISurface~Evaluate.html</para>
    /// </summary>
    public static bool EvaluateSurface(object surface, double u, double v, out Vector3D position, out Vector3D normal)
    {
        position = new Vector3D();
        normal = new Vector3D();

        if (surface == null)
            return false;

        try
        {
            dynamic s = surface;
            var evaluation = s.Evaluate(u, v, 1, 1) as double[];
            if (evaluation == null || evaluation.Length < 9)
                return false;

            position = new Vector3D(evaluation[0], evaluation[1], evaluation[2]);
            var tangentU = new Vector3D(evaluation[3], evaluation[4], evaluation[5]).Normalize();
            var tangentV = new Vector3D(evaluation[6], evaluation[7], evaluation[8]).Normalize();
            normal = tangentU.Cross(tangentV).Normalize();

            return normal.Length > 1e-9;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region IMathUtility API

    /// <summary>
    /// Creates a transform from a 16-element array.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IMathUtility~CreateTransform.html</para>
    /// </summary>
    public static MathTransform? CreateTransform(IMathUtility mathUtility, double[] data)
    {
        if (mathUtility == null || data == null || data.Length != 16)
            return null;
        
        try
        {
            return mathUtility.CreateTransform(data) as MathTransform;
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region IPartDoc API

    /// <summary>
    /// Creates a feature from a body.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IPartDoc~CreateFeatureFromBody3.html</para>
    /// </summary>
    public static object? CreateFeatureFromBody(IPartDoc partDoc, IBody2 body)
    {
        if (partDoc == null || body == null) return null;
        try
        {
            return partDoc.CreateFeatureFromBody3(body, false, 0);
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region IModelDoc2 API

    /// <summary>
    /// Clears the selection.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IModelDoc2~ClearSelection2.html</para>
    /// </summary>
    public static void ClearSelection(IModelDoc2 doc)
    {
        if (doc == null) return;
        try
        {
            doc.ClearSelection2(true);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Inserts an imported body.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IModelDocExtension~InsertImportedBody2.html</para>
    /// </summary>
    public static bool InsertImportedBody(IModelDoc2 doc, IBody2 body)
    {
        if (doc == null || body == null) return false;
        try
        {
            dynamic docExt = doc;
            docExt.InsertImportedBody2(body, false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region ISldWorks API

    /// <summary>
    /// Gets the math utility.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISldWorks~GetMathUtility.html</para>
    /// </summary>
    public static IMathUtility? GetMathUtility(SldWorks swApp)
    {
        if (swApp == null) return null;
        try
        {
            return swApp.GetMathUtility() as IMathUtility;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the active document.
    /// <para>Document: https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISldWorks~IActiveDoc2.html</para>
    /// </summary>
    public static IModelDoc2? GetActiveDoc(SldWorks swApp)
    {
        if (swApp == null) return null;
        try
        {
            return swApp.IActiveDoc2 as IModelDoc2;
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region COM Object Management

    /// <summary>
    /// Releases a COM object safely.
    /// </summary>
    public static void ReleaseComObject(object? obj)
    {
        if (obj != null && Marshal.IsComObject(obj))
        {
            try
            {
                Marshal.ReleaseComObject(obj);
            }
            catch
            {
            }
        }
    }

    #endregion
}