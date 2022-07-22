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

function Invoke-StageDependencies {
    exec {
        dotnet tool restore
    }
    exec {
        dotnet restore
    }
}

function Invoke-StageBuild {
    exec {
        dotnet build --configuration Release
    }
}

function Invoke-StageTest {
    # execute unit tests and gather code coverage statistics.
    # see https://github.com/spekt/junit.testlogger/blob/master/docs/gitlab-recommendation.md
    # see https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md
    exec {
        dotnet test `
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
