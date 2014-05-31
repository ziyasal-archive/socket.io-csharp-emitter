SET MsBuildPath="C:\Program Files (x86)\MSBuild\12.0\Bin"
SET NuGetExe=.nuget\nuget.exe
%MsBuildPath%\MsBuild.exe build.proj

.nuget\NuGet.exe pack SocketIO.Emitter\SocketIO.Emitter.nuspec