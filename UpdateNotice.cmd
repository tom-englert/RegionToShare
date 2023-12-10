dotnet tool update TomsToolbox.LicenseGenerator --global
build-license -i "%~dp0src\RegionToShare.sln" -o .\NOTICE.TXT --recursive
