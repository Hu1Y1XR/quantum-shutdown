# ============================================================
#  QUANTUM SHUTDOWN 构建脚本
#  使用 Windows 自带 csc.exe 编译，不需要安装任何 SDK
# ============================================================
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

# --- 1. 确保 App.cs 为 UTF-8 BOM（csc 依赖 BOM 识别中文） ---
$cs = Join-Path $dir 'App.cs'
$bytes = [IO.File]::ReadAllBytes($cs)
if (-not ($bytes.Length -gt 2 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) {
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    [IO.File]::WriteAllText($cs, $text, (New-Object System.Text.UTF8Encoding($true)))
    Write-Host 'App.cs: UTF-8 BOM added'
}

# --- 2. 生成程序图标（电源符号） ---
Add-Type -AssemblyName System.Drawing
$ico = Join-Path $dir 'icon.ico'
$bmp = New-Object System.Drawing.Bitmap 64, 64
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)
$bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 11, 18, 38))
$g.FillEllipse($bg, 1, 1, 62, 62)
$cyan = [System.Drawing.Color]::FromArgb(255, 43, 228, 255)
$pen = New-Object System.Drawing.Pen ($cyan), 5
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($pen, 15, 15, 34, 34, -55, 290)   # 圆环，顶部留缺口
$g.DrawLine($pen, 32, 7, 32, 25)             # 电源竖线
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = [IO.File]::Create($ico)
$icon.Save($fs); $fs.Close()
$g.Dispose(); $bmp.Dispose()
Write-Host 'icon.ico: generated'

# --- 3. 编译 ---
$fw  = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
$wpf = Join-Path $fw 'WPF'
if (-not (Test-Path $csc)) { throw "未找到 csc.exe: $csc" }

$out = Join-Path $dir 'ShutdownConsole_tmp.exe'
if (Test-Path $out) { Remove-Item $out -Force }

& $csc /nologo /target:winexe /platform:anycpu /optimize+ `
    /win32icon:"$ico" `
    /out:"$out" `
    /r:"$wpf\PresentationCore.dll" `
    /r:"$wpf\PresentationFramework.dll" `
    /r:"$wpf\WindowsBase.dll" `
    /r:"$fw\System.Xaml.dll" `
    /r:"$fw\System.dll" `
    /r:"$fw\System.Core.dll" `
    "$cs"
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

# --- 4. 重命名为最终中文名 ---
$final = Join-Path $dir '智能关机控制台.exe'
if (Test-Path $final) { Remove-Item $final -Force }
Move-Item $out $final
Write-Host "BUILD OK -> $final"
