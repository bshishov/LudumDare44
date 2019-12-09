// Repository: https://github.com/bshishov/JenkinsShared
// git (https): https://github.com/bshishov/JenkinsShared.git
@Library('JenkinsShared')_

pipeline {
	agent { label 'unity3d' }
	environment {		
		NAME = 'GraveRunner'				
		NEXUS_URL = "https://nexus.shishov.me"
	}
	stages {		
		stage('Build win_x86') {
			environment {
				PLATFORM = 'win_x86'
				BUILD_FILENAME = "${env.NAME}_${env.BRANCH_NAME}_${env.PLATFORM}.zip"
			}
			steps {
				buildUnity3d(
					projectPath: env.WORKSPACE, 
					buildArgs: "-buildTarget Win -buildWindowsPlayer \"${env.WORKSPACE}/builds/${env.PLATFORM}/${env.NAME}.exe\""
				)
				zipDirectory directory: "builds/${env.PLATFORM}/", file: "builds/${env.BUILD_FILENAME}"
			}			
		}
		stage('Build win_x64') {
			environment {
				PLATFORM = 'win_x64'
				BUILD_FILENAME = "${env.NAME}_${env.BRANCH_NAME}_${env.PLATFORM}.zip"
			}
			steps {
				buildUnity3d(
					projectPath: env.WORKSPACE, 
					buildArgs: "-buildTarget Win64 -buildWindows64Player \"${env.WORKSPACE}/builds/${env.PLATFORM}/${env.NAME}.exe\""
				)
				zipDirectory directory: "builds/${env.PLATFORM}/", file: "builds/${env.BUILD_FILENAME}"
			}			
		}
		// TODO: more platforms
		stage('Nexus upload') {
			steps {
				script {
					artifacts = findFiles(glob: 'builds/*.zip')
					// TODO: make parallel
					artifacts.each {
						echo "Uploading ${it.name} to nexus"
						uploadToNexus3(
							filename: it.path,
							nexusUrl: env.NEXUS_URL,
							targetFilename: "${env.NAME}/${it.name}",
							credentialsId: 'nexus-publish'
						)
					}		
				}		
			}
		}
	}
	post {
		always {			
			cleanWs()
		}
	}
}
