$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) {
    throw 'Run this script from 64-bit PowerShell.'
}

$installDirectory = Join-Path $env:LOCALAPPDATA 'NJSTools\InventorAddIn'
$addinManifest = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins\NJS.InventorAddIn.addin'
$classId = '{D4E8B219-63C7-4A92-9B15-8E24F610C3A7}'
$progId = 'NJS.InventorAddIn.StandardAddInServer'

$classesKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Classes', $true)
if ($classesKey) {
    try {
        $classesKey.DeleteSubKeyTree("CLSID\$classId", $false)
        $classesKey.DeleteSubKeyTree($progId, $false)
    }
    finally {
        $classesKey.Dispose()
    }
}

if (Test-Path $addinManifest) {
    Remove-Item $addinManifest -Force
}

if (Test-Path $installDirectory) {
    Remove-Item $installDirectory -Recurse -Force
}

Write-Host 'NJS Tools Inventor add-in removed for the current Windows user.'
Write-Host 'Restart Inventor if it was open during removal.'
