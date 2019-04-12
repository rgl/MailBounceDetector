// example to use in the https://github.com/rgl/jenkins-vagrant environment.
pipeline {
    agent {
        label 'vs2019'
    }
    stages {
        stage('Build') {
            steps {
                bat 'MSBuild -m -p:Configuration=Release -t:restore -t:build'
            }
        }
        stage('Test') {
            steps {
                powershell '''
                    Set-StrictMode -Version Latest
                    $ErrorActionPreference = 'Stop'
                    $ProgressPreference = 'SilentlyContinue'
                    trap {
                        Write-Output "ERROR: $_"
                        Write-Output (($_.ScriptStackTrace -split '\\r?\\n') -replace '^(.*)$','ERROR: $1')
                        Write-Output (($_.Exception.ToString() -split '\\r?\\n') -replace '^(.*)$','ERROR EXCEPTION: $1')
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

                    dir -Recurse */bin/*.Tests.dll | ForEach-Object {
                        Push-Location $_.Directory
                        Write-Host "Running the unit tests in $($_.Name)..."
                        exec {
                            # NB maybe you should also use -skipautoprops
                            OpenCover.Console.exe `
                                -output:opencover-report.xml `
                                -register:path64 `
                                '-filter:+[*]* -[*.Tests*]* -[*]*.*Config -[xunit.*]*' `
                                '-target:xunit.console.exe' `
                                "-targetargs:$($_.Name) -nologo -noshadow -xml xunit-report.xml"
                        }
                        exec {
                            ReportGenerator.exe `
                                -reports:opencover-report.xml `
                                -targetdir:coverage-report
                        }
                        Compress-Archive `
                            -CompressionLevel Optimal `
                            -Path coverage-report/* `
                            -DestinationPath coverage-report.zip
                        Pop-Location
                    }
                    '''
                xunit tools: [xUnitDotNet(pattern: '**/xunit-report.xml')],
                    thresholds: [skipped(failureThreshold: '0'), failed(failureThreshold: '0')],
                    thresholdMode: 1,
                    testTimeMargin: '3000'
                // when there are tests failures, the previous xunit step only marks
                // the build as failed and does not abort it. this step will really
                // abort it.
                // see https://github.com/jenkinsci/xunit-plugin/pull/62
                script {
                    if (currentBuild.result == 'FAILURE') {
                        error 'Aborting the build due to test failures...'
                    }
                }
            }
        }
    }
    post {
        success {
            archiveArtifacts '**/*.nupkg,**/*-report.*'
        }
        always {
            step $class: 'Mailer',
                recipients: emailextrecipients([
                    culprits(),
                    requestor()
                ]),
                notifyEveryUnstableBuild: true,
                sendToIndividuals: false
        }
    }
}
