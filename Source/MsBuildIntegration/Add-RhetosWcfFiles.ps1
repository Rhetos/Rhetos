# Documentation:
# https://docs.microsoft.com/en-us/nuget/reference/ps-reference/ps-ref-get-project
# https://docs.microsoft.com/en-us/dotnet/api/envdte.dte
# https://docs.microsoft.com/en-us/dotnet/api/envdte.projectitems.addfromfilecopy?view=visualstudiosdk-2017

$sourceFolder = "$PSScriptRoot\projectFiles"
$project = (Get-Project)
$projectFolder = (Get-Item $project.FullName).DirectoryName
"Source folder: $sourceFolder"

Copy-Item -Path "$sourceFolder\Web.config" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Rhetos Server DOM.linq" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Rhetos Server SOAP.linq" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Template.ConnectionStrings.config" -Destination $projectFolder -Force

$project.ProjectItems.AddFromFileCopy("$sourceFolder\RhetosService.svc") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\Global.asax") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\Default.aspx") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\RhetosRuntime.cs") > $null

function ReplaceText
{
    param([string]$file, [string]$pattern, [string]$replacement)
    $content = (Get-Content -Path $file -Raw)
    $content = $content -Replace $pattern,$replacement
    Set-Content -Path $file -Value $content -NoNewline
}

$assemblyName = $project.Properties["AssemblyName"].Value
ReplaceText "$projectFolder\RhetosService.svc" ", Rhetos" ", $assemblyName"

ReplaceText "$projectFolder\Rhetos Server DOM.linq" "bin\\Plugins\\" "bin\"
ReplaceText "$projectFolder\Rhetos Server SOAP.linq" "bin\\Plugins\\" "bin\"

ReplaceText "$projectFolder\Rhetos Server DOM.linq" "bin\\Generated\\ServerDom.Model.dll" "bin\$assemblyName.dll"
ReplaceText "$projectFolder\Rhetos Server SOAP.linq" "bin\\Generated\\ServerDom.Model.dll" "bin\$assemblyName.dll"
ReplaceText "$projectFolder\Rhetos Server SOAP.linq" "ServerDom.Model" "$assemblyName"

ReplaceText "$projectFolder\Rhetos Server SOAP.linq" "localhost/Rhetos" "ENTER-APPLICATION-URL-HERE"

$rhetosAppSettingsPath = "$projectFolder\rhetos-app.settings.json"
@"
{
  "RhetosRuntimePath": "bin\\$assemblyName.dll",
  "AssetsFolder": "bin\\RhetosAssets",
  "DatabaseLanguage": "MsSql",
  "Legacy":  {
    "Paths": {
        "ResourcesFolder": "Resources"
    }
  }
}
"@ | Set-Content -Path $rhetosAppSettingsPath
$project.ProjectItems.AddFromFile($rhetosAppSettingsPath) > $null

$rhetosBuildSettingsPath = "$projectFolder\rhetos-build.settings.json"
@"
{
  "GenerateAppSettings": false,
  "Legacy": {
    "BuildResourcesFolder": true
  }
}
"@ | Set-Content -Path $rhetosBuildSettingsPath
$project.ProjectItems.AddFromFile($rhetosBuildSettingsPath) > $null