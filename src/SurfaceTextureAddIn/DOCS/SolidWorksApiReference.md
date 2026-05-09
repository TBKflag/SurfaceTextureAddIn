# SolidWorks API 使用规范

## 概述

本文档记录项目中使用的 SolidWorks API 方法，确保所有 API 调用都有官方文档依据。

---

## 已验证的 API 列表

### 1. IBody2 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `Copy2(bool PreserveFaceIDs)` | PreserveFaceIDs: 是否保留面 ID | IBody2 | [Copy2 Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~Copy2.html) |
| `Operations2(int Operation, IBody2 ToolBody, out int ErrorCode)` | Operation: 操作类型, ToolBody: 工具体, ErrorCode: 错误码 | object[] | [Operations2 Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~Operations2.html) |
| `ApplyTransform(MathTransform Transform)` | Transform: 变换矩阵 | void | [ApplyTransform Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~ApplyTransform.html) |
| `GetFaces()` | 无 | object[] | [GetFaces Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~GetFaces.html) |
| `Name` (属性) | 无 | string | [Name Property](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IBody2~Name.html) |

### 2. IFace2 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `GetUVBounds()` | 无 | double[] (minU, maxU, minV, maxV) | [GetUVBounds Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFace2~GetUVBounds.html) |
| `GetSurface()` | 无 | ISurface | [GetSurface Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFace2~GetSurface.html) |

### 3. ISurface 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `Evaluate(double U, double V, int DerivativeOrderU, int DerivativeOrderV)` | U, V: 参数坐标, DerivativeOrder: 导数阶数 | double[] (X,Y,Z,dXdu,dYdu,dZdu,dXdv,dYdv,dZdv) | [Evaluate Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISurface~Evaluate.html) |

### 4. IMathUtility 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `CreateTransform(double[] Data)` | Data: 16元素变换矩阵 | MathTransform | [CreateTransform Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IMathUtility~CreateTransform.html) |

### 5. IPartDoc 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `CreateFeatureFromBody3(IBody2 Body, bool KeepVisible, int Option)` | Body: 体, KeepVisible: 是否保持可见, Option: 选项 | IFeature | [CreateFeatureFromBody3 Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IPartDoc~CreateFeatureFromBody3.html) |

### 6. IModelDoc2 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `ClearSelection2(bool Append)` | Append: 是否追加选择 | bool | [ClearSelection2 Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IModelDoc2~ClearSelection2.html) |
| `InsertImportedBody2(IBody2 Body, bool KeepVisible)` | Body: 体, KeepVisible: 是否保持可见 | bool | [InsertImportedBody2 Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IModelDocExtension~InsertImportedBody2.html) |

### 7. ISldWorks 接口

| 方法名 | 参数 | 返回值 | 文档链接 |
|--------|------|--------|----------|
| `GetMathUtility()` | 无 | IMathUtility | [GetMathUtility Method](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISldWorks~GetMathUtility.html) |
| `IActiveDoc2` (属性) | 无 | IModelDoc2 | [IActiveDoc2 Property](https://help.solidworks.com/2020/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISldWorks~IActiveDoc2.html) |

---

## 禁止使用的方法

以下方法经确认不存在于 SolidWorks 2020 API 中：

| 方法名 | 错误原因 |
|--------|----------|
| `IFace2.GetCenter()` | 不存在此方法，应使用 `GetUVBounds()` + `ISurface.Evaluate()` 获取中心点 |
| `IFace2.Normal` (属性) | 不存在此属性，应从切向量叉乘计算法向量 |

---

## 代码审查规则

1. **所有 API 调用必须有文档依据**：新增 API 调用时，必须在此文档中添加对应的条目和官方文档链接

2. **优先使用强类型接口**：尽可能使用 `IBody2`、`IFace2` 等强类型接口，避免使用 `dynamic` 调用未知方法

3. **错误处理**：所有 API 调用必须包含错误检查和异常处理

4. **COM 对象释放**：使用完 COM 对象后必须调用 `Marshal.ReleaseComObject()` 释放资源

5. **参数验证**：调用 API 前必须验证参数有效性

6. **未知 API 确认流程**：如果在此文档中找不到对应的 API，必须到 SolidWorks 2020 官方 API 文档网站（[help.solidworks.com/2020/english/api/sldworksapi](https://help.solidworks.com/2020/english/api/sldworksapi)）搜索确认，确保使用正确的接口方法

---

## 使用示例

### 正确：获取面中心点
```csharp
dynamic face = targetFace;
var uv = (double[]?)face.GetUVBounds();
var centerU = (uv[0] + uv[1]) / 2;
var centerV = (uv[2] + uv[3]) / 2;
dynamic surface = face.GetSurface();
var eval = surface.Evaluate(centerU, centerV, 1, 1);
var point = new Vector3D(eval[0], eval[1], eval[2]);
```

### 错误：使用不存在的方法
```csharp
// 错误：GetCenter() 不存在
var center = face.GetCenter();
```

---

## 版本说明

- **SolidWorks 版本**：2020 SP5
- **API 文档版本**：2020
- **最后更新**：2026-05-09