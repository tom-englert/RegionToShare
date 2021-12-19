setlocal

set pad="..\ImagePadding\bin\debug\net6.0\ImagePadding.exe"

for %%a in (16,24,32,48,256) do copy "..\Assets\%%a.png" "Assets\Square44x44Logo.targetsize-%%a.png"
for %%a in (16,24,32,48,256) do copy "..\Assets\%%a.png" "Assets\Square44x44Logo.altform-unplated_targetsize-%%a.png"

%pad% ..\Assets\32.png 71 "Assets\SmallTile.scale-100.png"
%pad% ..\Assets\32.png 89 "Assets\SmallTile.scale-125.png"
%pad% ..\Assets\48.png 107 "Assets\SmallTile.scale-150.png"
%pad% ..\Assets\64.png 142 "Assets\SmallTile.scale-200.png"
%pad% ..\Assets\128.png 284 "Assets\SmallTile.scale-400.png"

%pad% ..\Assets\64.png 150 "Assets\MediumTile.scale-100.png"
%pad% ..\Assets\64.png 188 "Assets\MediumTile.scale-125.png"
%pad% ..\Assets\128.png 225 "Assets\MediumTile.scale-150.png"
%pad% ..\Assets\128.png 300 "Assets\MediumTile.scale-200.png"
%pad% ..\Assets\256.png 600 "Assets\MediumTile.scale-400.png"

%pad% ..\Assets\32.png 44 "Assets\Square44x44Logo.scale-100.png"
%pad% ..\Assets\32.png 55 "Assets\Square44x44Logo.scale-125.png"
%pad% ..\Assets\48.png 66 "Assets\Square44x44Logo.scale-150.png"
%pad% ..\Assets\64.png 88 "Assets\Square44x44Logo.scale-200.png"
%pad% ..\Assets\128.png 176 "Assets\Square44x44Logo.scale-400.png"

%pad% ..\Assets\48.png 50 "Assets\PackageLogo.scale-100.png"
%pad% ..\Assets\48.png 63 "Assets\PackageLogo.scale-125.png"
%pad% ..\Assets\64.png 75 "Assets\PackageLogo.scale-150.png"
%pad% ..\Assets\64.png 100 "Assets\PackageLogo.scale-200.png"
%pad% ..\Assets\128.png 200 "Assets\PackageLogo.scale-400.png"
