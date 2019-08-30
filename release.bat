@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

rmdir /s /q releases 
md releases

rmdir /s /q .\EOSEasyContract\bin\release 

set winOS=win-x64
; set unixOS=osx-x64 linux-x64

for %%N in (%winOS%) do (
	set rid=%%N
	dotnet publish ./EOSEasyContract/EOSEasyContract.csproj -c release -r !rid!
	7z a -tzip ./releases/EOSEasyContract-!rid!.zip ./EOSEasyContract/bin/release/netcoreapp2.1/!rid!/publish/* -r
)

for %%N in (%unixOS%) do (
	set rid=%%N
	dotnet publish ./EOSEasyContract/EOSEasyContract.csproj -c release -r !rid!
	7z a -ttar -so ./releases/EOSEasyContract-!rid!.tar ./EOSEasyContract/bin/release/netcoreapp2.1/!rid!/publish/* -r | 7z a -si ./releases/EOSEasyContract-!rid!.tar.gz
)
