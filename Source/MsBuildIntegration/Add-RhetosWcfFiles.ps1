param([string]$Project)

function AddItemToCsprojFile {
 param( [string]$fileName, [string]$itemType, [System.Xml.XmlDocument]$csprojFile, [string]$xmlFragment)

    $MsbNS = @{msb = 'http://schemas.microsoft.com/developer/msbuild/2003'}
    $itemGroup = Select-Xml -Xml $csprojFile -XPath "//msb:Project/msb:ItemGroup/msb:$itemType" -Namespace $MsbNS | Foreach-Object {$_.Node.ParentNode} | Select-Object -First 1
    
    if(@(Select-Xml -Xml $csprojFile -XPath "//msb:Project/msb:ItemGroup/msb:$itemType[@Include='$fileName']" -Namespace $MsbNS | Foreach-Object {$_.Node.ParentNode} | Select-Object -First 1).Length -eq 0)
    {
        Write-Host 'Creating item element for file '$fileName'.'

        $xmlFragment = If (-Not [string]::IsNullOrEmpty($xmlFragment)) {$xmlFragment} Else {""}
        $xmlNodeText = "<$($itemType) Include=`"$($fileName)`" xmlns=`"$($MsbNS.msb)`">$($xmlFragment)</$($itemType)>"
        
        $xmlToAdd = New-Object System.Xml.XmlDocument;
        $xmlToAdd.LoadXml($xmlNodeText);

        $nodeToAdd = $csprojFile.ImportNode($xmlToAdd.DocumentElement, $TRUE)
        if($NULL -ne $nodeToAdd.Attributes  -and $NULL -ne $nodeToAdd.Attributes["xmlns"])
        {
            $nodeToAdd.Attributes.Remove($nodeToAdd.Attributes["xmlns"]) | Out-Null
        }
        $itemGroup.AppendChild($nodeToAdd) | Out-Null
    }
    else
    {
        Write-Host 'Skipping creating item element for file '$fileName' because one was already found.'
    }
}

function CopyToProjectFolder {
 param( [string]$fileName, [string]$contentPath, [string]$projectPath)
    $sourceFilePath = $contentPath+'\'+$fileName;
    $destinationFilePath = $projectPath+'\'+$fileName;
    if(Test-Path $destinationFilePath)
    {
        Write-Host 'Overwriting the file '$destinationFilePath
    }
    else{
        Write-Host 'Copying file '$fileName' to '$destinationFilePath
    }
    Copy-Item -Path $sourceFilePath -Destination $destinationFilePath -Force
}

if([string]::IsNullOrEmpty($Project)){
    Write-Host 'Project name is not set. Use Add-RhetosWcfFiles <ProjectName> where ProjectName is the name of the project on which you want to execute the script.' -ForegroundColor red
    exit
}

$csprojFiles = @(Get-ChildItem $pwd -Filter $Project'.csproj' -Recurse)
if($csprojFiles.Length -gt 1)
{
    Write-Host 'Found multiple project with the same name. Navigate to the project directory and call Add-RhetosWcfFiles '$Project -ForegroundColor red
    exit
}

if($csprojFiles.Length -eq 0)
{
    Write-Host 'Could not find project with the name '$Project'.' -ForegroundColor red
    exit
}

$contentPath = $PSScriptRoot + '\projectFiles'
$csprojFilePath = $csprojFiles | Select-Object -First 1
$csprojFile = New-Object System.Xml.XmlDocument
$csprojFile.Load($csprojFilePath.FullName)

CopyToProjectFolder 'RhetosService.svc' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'RhetosService.svc' 'Content' $csprojFile;
CopyToProjectFolder 'RhetosService.svc.cs' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'RhetosService.svc.cs' 'Compile' $csprojFile '<DependentUpon>RhetosService.svc</DependentUpon>';

CopyToProjectFolder 'Global.asax' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'Global.asax' 'Content' $csprojFile;
CopyToProjectFolder 'Global.asax.cs' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'Global.asax.cs' 'Compile' $csprojFile '<DependentUpon>Global.asax</DependentUpon>';

CopyToProjectFolder 'Default.aspx' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'Default.aspx' 'Content' $csprojFile;
CopyToProjectFolder 'Default.aspx.cs' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'Default.aspx.cs' 'Compile' $csprojFile '<DependentUpon>Default.aspx</DependentUpon><SubType>ASPXCodeBehind</SubType>';

CopyToProjectFolder 'Web.config' $contentPath $csprojFilePath.PSParentPath
AddItemToCsprojFile 'Web.config' 'Content' $csprojFile '<SubType>Designer</SubType>'

CopyToProjectFolder 'Rhetos Server DOM.linq' $contentPath $csprojFilePath.PSParentPath
CopyToProjectFolder 'Rhetos Server SOAP.linq' $contentPath $csprojFilePath.PSParentPath
CopyToProjectFolder 'Template.ConnectionStrings.config' $contentPath $csprojFilePath.PSParentPath

$csprojFile.Save($csprojFilePath.FullName);