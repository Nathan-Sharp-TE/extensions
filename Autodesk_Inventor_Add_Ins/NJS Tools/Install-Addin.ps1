$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) {
    throw "This installer must be run from 64-bit PowerShell. The current process is 32-bit, so Autodesk Inventor 2026 and the x64 .NET build are not supported."
}

$inventorBinPath = 'C:\Program Files\Autodesk\Inventor 2026\Bin'
if (-not (Test-Path (Join-Path $inventorBinPath 'Autodesk.Inventor.Interop.dll'))) {
    throw "Inventor 2026 was not detected at '$inventorBinPath'. Install Autodesk Inventor 2026 before running this installer, or update the project build to the correct Inventor bin path."
}

$tempDir = $null
try {
    $localProjectFile = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $null
    } else {
        Join-Path $PSScriptRoot 'NJS.InventorAddIn.csproj'
    }

    if (-not $localProjectFile -or -not (Test-Path $localProjectFile)) {
        $tempDir = Join-Path $env:TEMP ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        $zipPath = Join-Path $tempDir 'repo.zip'
        Write-Host 'Downloading source from GitHub...'
        Invoke-RestMethod -Uri 'https://github.com/nathan-sharp/extensions/archive/refs/heads/main.zip' -OutFile $zipPath
        
        Write-Host 'Extracting source...'
        Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force
        
        $projectDir = Join-Path $tempDir 'extensions-main\Autodesk_Inventor_Add_Ins\NJS Tools'
        if (-not (Test-Path $projectDir)) {
            throw "The downloaded GitHub archive did not contain the expected project path: '$projectDir'. The repository layout may have changed; please build from source manually or verify the GitHub URL and folder names."
        }
        
        Write-Host 'Building project...'
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = 'dotnet'
        $pinfo.Arguments = "build `"$projectDir\NJS.InventorAddIn.csproj`" -c Release /nodeReuse:false"
        $pinfo.UseShellExecute = $false
        $pinfo.CreateNoWindow = $true
        $pinfo.RedirectStandardInput = $true
        $pinfo.RedirectStandardOutput = $true
        $pinfo.RedirectStandardError = $true
        $p = [System.Diagnostics.Process]::Start($pinfo)
        $p.StandardInput.Close()
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()
        if ($p.ExitCode -ne 0) {
            throw "Build failed with exit code $($p.ExitCode). This usually means the .NET SDK is missing, the Inventor interop files are unavailable, or the project source is incomplete. Build output: $stdout $stderr"
        }
        
        $buildDirectory = Join-Path $projectDir 'bin\Release\net8.0-windows'
        $templatePath = Join-Path $projectDir 'NJS.InventorAddIn.addin.template'
    } else {
        $buildDirectory = Join-Path $PSScriptRoot 'bin\Release\net8.0-windows'
        $templatePath = Join-Path $PSScriptRoot 'NJS.InventorAddIn.addin.template'
    }

    $assemblyPath = Join-Path $buildDirectory 'NJS.InventorAddIn.dll'
    $comHostPath = Join-Path $buildDirectory 'NJS.InventorAddIn.comhost.dll'

    foreach ($path in @($assemblyPath, $comHostPath, $templatePath)) {
        if (-not (Test-Path $path)) {
            $missingItem = Split-Path -Leaf $path
            if ($path -eq $assemblyPath) {
                throw "The compiled add-in assembly was not found: '$path'. Build the project in Release configuration first, and ensure the Inventor 2026 interop references are available."
            }
            if ($path -eq $comHostPath) {
                throw "The COM host DLL was not found: '$path'. The add-in build may have failed or the `EnableComHosting` output files were not generated. Check the Release build output directory."
            }
            if ($path -eq $templatePath) {
                throw "The add-in manifest template was not found: '$path'. The repository may not have been downloaded or extracted correctly. Check the project folder contents and confirm the template file exists."
            }
            throw "Required file not found: '$path'. Build the project in Release configuration first."
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
}
finally {
    if ($null -ne $tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
