#!/bin/sh
dotnet publish -c Release -r osx-x64 --self-contained false -o ./deploy/GMRES.Console-osx-x64/