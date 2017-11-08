# Try download NuGet.exe if do not exist.
param (
    [string] $nugetExePath,
    [string] $nugetExeUrl
)
if (Test-Path $nugetExePath) 
{
    Write-Output "Found '$nugetExePath'"
    return
}

if(!$nugetExeUrl) {
    $nugetExeUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
}

Write-Output "Download nuget.exe from '$nugetExeUrl'"
Invoke-WebRequest -Uri $nugetExeUrl -OutFile $nugetExePath
