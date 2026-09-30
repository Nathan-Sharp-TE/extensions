$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) {
    throw 'Run this script from 64-bit PowerShell.'
}

$buildDirectory = Join-Path $PSScriptRoot 'bin\Release\net8.0-windows'
$assemblyPath = Join-Path $buildDirectory 'NJS.InventorAddIn.dll'
$comHostPath = Join-Path $buildDirectory 'NJS.InventorAddIn.comhost.dll'
$templatePath = Join-Path $PSScriptRoot 'NJS.InventorAddIn.addin.template'

foreach ($path in @($assemblyPath, $comHostPath, $templatePath)) {
    if (-not (Test-Path $path)) {
        throw "Required file not found: $path. Build the project in Release configuration first."
    }
}

$addinDirectory = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins'
$installRoot = Join-Path $env:LOCALAPPDATA 'NJSTools\InventorAddIn'
$buildId = (Get-FileHash -Path $assemblyPath -Algorithm SHA256).Hash.Substring(0, 12)
$installDirectory = Join-Path $installRoot $buildId
New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $addinDirectory -Force | Out-Null

Copy-Item (Join-Path $buildDirectory '*') $installDirectory -Force
$installedAssembly = Join-Path $installDirectory 'NJS.InventorAddIn.dll'
$installedComHost = Join-Path $installDirectory 'NJS.InventorAddIn.comhost.dll'
$addinManifest = Join-Path $addinDirectory 'NJS.InventorAddIn.addin'
$classId = '{D4E8B219-63C7-4A92-9B15-8E24F610C3A7}'
$progId = 'NJS.InventorAddIn.StandardAddInServer'

$manifest = (Get-Content $templatePath -Raw).Replace('__ADDIN_ASSEMBLY_PATH__', $installedAssembly)
$classesKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes')
try {
    $classKey = $classesKey.CreateSubKey("CLSID\$classId")
    try {
        $classKey.SetValue('', 'NJS Tools Inventor Add-In', [Microsoft.Win32.RegistryValueKind]::String)
        $inprocKey = $classKey.CreateSubKey('InprocServer32')
        try {
            $inprocKey.SetValue('', $installedComHost, [Microsoft.Win32.RegistryValueKind]::String)
            $inprocKey.SetValue('ThreadingModel', 'Both', [Microsoft.Win32.RegistryValueKind]::String)
        }
        finally {
            $inprocKey.Dispose()
        }

        $classProgIdKey = $classKey.CreateSubKey('ProgID')
        try {
            $classProgIdKey.SetValue('', $progId, [Microsoft.Win32.RegistryValueKind]::String)
        }
        finally {
            $classProgIdKey.Dispose()
        }
    }
    finally {
        $classKey.Dispose()
    }

    $progIdKey = $classesKey.CreateSubKey($progId)
    try {
        $progIdKey.SetValue('', 'NJS Tools Inventor Add-In', [Microsoft.Win32.RegistryValueKind]::String)
        $progIdClsidKey = $progIdKey.CreateSubKey('CLSID')
        try {
            $progIdClsidKey.SetValue('', $classId, [Microsoft.Win32.RegistryValueKind]::String)
        }
        finally {
            $progIdClsidKey.Dispose()
        }
    }
    finally {
        $progIdKey.Dispose()
    }

    Set-Content -Path $addinManifest -Value $manifest -Encoding UTF8
}
catch {
    $classesKey.DeleteSubKeyTree("CLSID\$classId", $false)
    $classesKey.DeleteSubKeyTree($progId, $false)
    Remove-Item $addinManifest -Force -ErrorAction SilentlyContinue
    throw "Per-user COM registration failed: $($_.Exception.Message)"
}
finally {
    $classesKey.Dispose()
}

Write-Host 'NJS Tools Inventor add-in installed for the current Windows user.'
Write-Host 'Restart Inventor 2026 to load it.'
