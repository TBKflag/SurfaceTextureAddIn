使用指引（VS2022 + SolidWorks）
1) 编译插件
在 VS2022 打开 SurfaceTextureAddIn.sln，选择 Debug|Any CPU（或 Release|Any CPU）后生成。
输出 DLL 在：

src/SurfaceTextureAddIn/bin/Debug/net48/SurfaceTextureAddIn.dll
或 .../Release/net48/SurfaceTextureAddIn.dll
2) 注册 COM 插件（首次/更新后）
先关闭 SolidWorks（避免 DLL 被占用导致编译失败），再以管理员身份打开 PowerShell 执行：

powershell -ExecutionPolicy Bypass -File .\register.ps1 -Configuration Debug -Framework net48 -SolidWorksInteropDir "D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"

如果用 Release，请把 `-Configuration` 改成 `Release`。
如果 SolidWorks 安装目录不是上面路径，请改 `-SolidWorksInteropDir`。

3) 在 SolidWorks 中启用
打开 SolidWorks
工具 -> 加载项(Add-Ins)
勾选 Surface Texture Add-In（可同时勾选“启动时加载”）
4) 使用流程
在零件里准备一个“纹理种子实体”（独立 solid body）。
预选：
1 个种子实体（body）
1 个目标面（face）
执行命令：
Generate Convex Texture（凸起）
Generate Concave Texture（凹陷）
调参数（间距、深高、边距、旋转、最大实例数、曲率过滤等）并确认。
5) 卸载/反注册（可选）
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" "D:\坚果云\我的坚果云\插件\src\SurfaceTextureAddIn\bin\Debug\net48\SurfaceTextureAddIn.dll" /unregister
如果你愿意，我可以下一步再帮你加一个“一键注册脚本”（register.ps1 / unregister.ps1），这样以后每次编译后双击脚本就能更新插件注册。