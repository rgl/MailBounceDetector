// example to use in the https://github.com/rgl/jenkins-vagrant environment.
pipeline {
    agent {
        label 'vs2017'
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
                    dir -Recurse */bin/*.Tests.dll | ForEach-Object {
                        Push-Location $_.Directory
                        Write-Host "Running the unit tests in $($_.Name)..."
                        # NB maybe you should also use -skipautoprops
                        OpenCover.Console.exe `
                            -output:opencover-report.xml `
                            -register:path64 `
                            '-filter:+[*]* -[*.Tests*]* -[*]*.*Config -[xunit.*]*' `
                            '-target:xunit.console.exe' `
                            "-targetargs:$($_.Name) -nologo -noshadow -xml xunit-report.xml"
                        ReportGenerator.exe `
                            -reports:opencover-report.xml `
                            -targetdir:coverage-report
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
                    if (currentBuild.result != 'SUCCESS') {
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
                recipients: 'jenkins@example.com',
                notifyEveryUnstableBuild: true,
                sendToIndividuals: false
        }
    }
}
