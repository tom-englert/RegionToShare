param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$OutDir
)

$ErrorActionPreference = 'Stop'

$baseUrl = "https://github.com/tom-englert/RegionToShare/releases/download/$Tag"
$assets = @(
    @{ Architecture = 'x64'; File = 'RegionToShare.exe' },
    @{ Architecture = 'arm64'; File = 'RegionToShare-arm64.exe' }
)

$tmp = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tmp | Out-Null

try {
    $installers = foreach ($asset in $assets) {
        $url = "$baseUrl/$($asset.File)"
        $file = Join-Path $tmp $asset.File
        Write-Host "Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $file -UseBasicParsing
        $hash = (Get-FileHash -Path $file -Algorithm SHA256).Hash
        "- Architecture: $($asset.Architecture)`n  InstallerUrl: $url`n  InstallerSha256: $hash"
    }
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$id = 'TomEnglert.RegionToShare'
$releaseDate = Get-Date -Format 'yyyy-MM-dd'
$manifestVersion = '1.12.0'

$versionYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$manifestVersion.schema.json

PackageIdentifier: $id
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $manifestVersion
"@

$installerYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$manifestVersion.schema.json

PackageIdentifier: $id
PackageVersion: $Version
InstallerLocale: en-US
InstallerType: portable
Commands:
- RegionToShare
ReleaseDate: $releaseDate
Installers:
$($installers -join "`n")
ManifestType: installer
ManifestVersion: $manifestVersion
"@

$localeYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$manifestVersion.schema.json

PackageIdentifier: $id
PackageVersion: $Version
PackageLocale: en-US
Publisher: Tom Englert
PublisherUrl: https://github.com/tom-englert/RegionToShare
PublisherSupportUrl: https://github.com/tom-englert/RegionToShare/issues
PackageName: Region To Share
PackageUrl: https://github.com/tom-englert/RegionToShare
License: GNU General Public License v3.0
LicenseUrl: https://github.com/tom-englert/RegionToShare/blob/main/LICENSE
ShortDescription: A Windows helper app to share only a part of a screen via video conference apps that only support either full screen or single window like e.g. Teams, WebEx, etc.
Description: A Windows helper app to share only a part of a screen via video conference apps that only support either full screen or single window like e.g. Teams, WebEx, etc. This tool simply mirrors the content of a screen region into a hidden window. In your meeting app you then can just share the content of this hidden window.
Tags:
- teams
ReleaseNotesUrl: https://github.com/tom-englert/RegionToShare/releases/tag/$Tag
ManifestType: defaultLocale
ManifestVersion: $manifestVersion
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $OutDir "$id.yaml"), $versionYaml + "`n", $utf8NoBom)
[IO.File]::WriteAllText((Join-Path $OutDir "$id.installer.yaml"), $installerYaml + "`n", $utf8NoBom)
[IO.File]::WriteAllText((Join-Path $OutDir "$id.locale.en-US.yaml"), $localeYaml + "`n", $utf8NoBom)
