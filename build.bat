@echo off
echo Restoring NuGet packages...
.\tools\nuget.exe restore .\src\SimpleZmq.sln

echo Building SimpleZmq...
pushd src
%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\msbuild build.proj /target:Build /p:Configuration=Release

echo Creating NuGet package...
..\tools\nuget.exe pack SimpleZmq.nuspec -OutputDirectory ..\build\
popd
