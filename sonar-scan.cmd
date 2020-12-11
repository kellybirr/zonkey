SET JAVA_HOME=C:\sonar-scanner\sonar-scanner-4.1.0.1829\jre
C:\sonar-scanner\SonarScanner.MSBuild.exe begin /k:"kellybirr_zonkey" /o:"kellybirr" /d:sonar.login="63db6b87bb6fc0ebd025048a2736cbb97cb0bd18"
MSBuild.exe "Zonkey 5.0.sln" /t:Rebuild
C:\sonar-scanner\SonarScanner.MSBuild.exe end /d:sonar.login="63db6b87bb6fc0ebd025048a2736cbb97cb0bd18"

;REM dotnet sonarscanner begin /k:"kellybirr_zonkey" /o:"kellybirr" /d:sonar.login="63db6b87bb6fc0ebd025048a2736cbb97cb0bd18" 
;REM dotnet build "Zonkey 5.0.sln"
;REM dotnet sonarscanner end /d:sonar.login="63db6b87bb6fc0ebd025048a2736cbb97cb0bd18" 
