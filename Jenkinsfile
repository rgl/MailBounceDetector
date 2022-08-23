// example to use in the https://github.com/rgl/jenkins-vagrant environment.
pipeline {
    agent {
        label 'vs2022'
    }
    stages {
        stage('Dependencies') {
            steps {
                powershell './build.ps1 dependencies'
            }
        }
        stage('Build') {
            steps {
                powershell './build.ps1 build'
            }
        }
        stage('Test') {
            steps {
                powershell './build.ps1 test'
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
