param(
    [Parameter(Mandatory=$true)]
    [string]$stage
)

Set-StrictMode -Version Latest
$FormatEnumerationLimit = -1
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
trap {
    "ERROR: $_" | Write-Host
    ($_.ScriptStackTrace -split '\r?\n') -replace '^(.*)$','ERROR: $1' | Write-Host
    ($_.Exception.ToString() -split '\r?\n') -replace '^(.*)$','ERROR EXCEPTION: $1' | Write-Host
    Exit 1
}

function exec([ScriptBlock]$externalCommand, [string]$stderrPrefix='', [int[]]$successExitCodes=@(0)) {
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        &$externalCommand 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) {
                "$stderrPrefix$_"
            } else {
                "$_"
            }
        }
        if ($LASTEXITCODE -notin $successExitCodes) {
            throw "$externalCommand failed with exit code $LASTEXITCODE"
        }
    } finally {
        $ErrorActionPreference = $eap
    }
}

function Get-SbomTool {
    $version = '0.2.6'
    $url = "https://github.com/microsoft/sbom-tool/releases/download/v$version/sbom-tool-win-x64.exe"
    $exe = "$PWD\bin\sbom-tool.exe"
    if (Test-Path $exe) {
        $actualVersion = (Get-ChildItem $exe).VersionInfo.ProductVersion
        if ($actualVersion -eq $version) {
            return
        }
    }
    Write-Host "Downloading $url..."
    mkdir -force bin | Out-Null
    (New-Object Net.WebClient).DownloadFile($url, $exe)
}

function Invoke-StageDependencies {
    exec {
        dotnet tool restore
    }
    exec {
        dotnet restore
    }
    Get-SbomTool
}

function Invoke-StageBuild {
    exec {
        dotnet build --no-restore --configuration Release
    }
    exec {
        $packageName = 'MailBounceDetector'
        $packagePath = Resolve-Path "$packageName/bin/Release/net6.0"
        $packageVersion = (Get-ChildItem "$packagePath/$packageName.dll").VersionInfo.ProductVersion
        $manifestPath = "$packagePath/_manifest"
        Remove-Item -Recurse -Force $manifestPath,$packagePath/../*.spdx.*
        mkdir $manifestPath | Out-Null
        .\bin\sbom-tool `
            generate `
            -ManifestDirPath $manifestPath `
            -BuildDropPath $packagePath `
            -BuildComponentPath $packageName `
            -PackageName $packageName `
            -PackageVersion $packageVersion `
            -PackageSupplier test.ruilopes.com `
            -NamespaceUriBase https://sbom.test.ruilopes.com/dotnet
        Get-ChildItem -Recurse -Include manifest.spdx.json $packagePath | ForEach-Object {
            Move-Item $_ "$packagePath/../$packageName.$packageVersion.spdx.json"
            Move-Item "$_.sha256" "$packagePath/../$packageName.$packageVersion.spdx.json.sha256"
        }
    }
}

function Invoke-StageTest {
    # execute unit tests and gather code coverage statistics.
    # see https://github.com/spekt/junit.testlogger/blob/master/docs/gitlab-recommendation.md
    # see https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md
    exec {
        dotnet test `
            --no-build `
            --configuration Release `
            --test-adapter-path . `
            --logger 'junit;MethodFormat=Class;FailureBodyFormat=Verbose' `
            --collect 'XPlat Code Coverage' `
            --settings MailBounceDetector.Tests/coverlet.runsettings
    }

    # generate coverage report.
    Get-ChildItem -Recurse TestResults.xml | ForEach-Object {
        Push-Location $_.Directory
        Write-Host "Running the unit tests in $($_.Name)..."
        exec {
            dotnet tool run reportgenerator `
                "-reports:$(Resolve-Path */coverage.opencover.xml)" `
                -targetdir:coverage-report
        }
        Compress-Archive `
            -CompressionLevel Optimal `
            -Path coverage-report/* `
            -DestinationPath coverage-report.zip
        Pop-Location
    }
}

Invoke-Expression "Invoke-Stage$([System.Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($stage))"
