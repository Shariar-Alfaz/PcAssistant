param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\PcAssistant\PcAssistant\Platforms\Windows\Api")
)

$ErrorActionPreference = "Stop"

$apiDirectory = $PSScriptRoot
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$buildDirectory = Join-Path $apiDirectory ".pyinstaller\build-$stamp"
$distDirectory = Join-Path $apiDirectory ".pyinstaller\dist-$stamp"
$specDirectory = Join-Path $apiDirectory ".pyinstaller\spec-$stamp"
$modelPath = Join-Path $apiDirectory "model\pc_assistant_command_classifier_best.joblib"
$python = Get-Command py -ErrorAction SilentlyContinue

if ($null -eq $python) {
    $python = Get-Command python -ErrorAction Stop
    $pythonExecutable = $python.Source
    $pythonArguments = @()
}
else {
    $pythonExecutable = $python.Source
    $pythonArguments = @("-3")
}

function Invoke-Python {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & $pythonExecutable @pythonArguments @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Python command failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

Push-Location $apiDirectory
try {
    Invoke-Python -m pip install -r requirements.txt
    Invoke-Python -m pip install pyinstaller
    Invoke-Python -m PyInstaller `
        --onedir `
        --optimize 2 `
        --name pcAssistantApi `
        --workpath $buildDirectory `
        --distpath $distDirectory `
        --specpath $specDirectory `
        --add-data "${modelPath};model" `
        main.py

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    Copy-Item -Recurse -Force -Path (Join-Path $distDirectory "pcAssistantApi\*") -Destination $OutputDirectory
}
finally {
    Pop-Location
}
