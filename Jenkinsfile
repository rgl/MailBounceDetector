// example to use in the https://github.com/rgl/jenkins-vagrant environment.
pipeline {
    agent {
        label 'vs2022'
    }
    stages {
        stage('Build') {
            steps {
                powershell './build.ps1 build'
            }
        }
        stage('Test') {
            steps {
                powershell './build.ps1 test'
                xunit tools: [JUnit(pattern: '**/TestResults.xml')],
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
