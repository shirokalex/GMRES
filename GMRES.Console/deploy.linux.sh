#!/bin/sh
dotnet publish -c Release -r linux-x64 --self-contained false -o ./deploy/GMRES.Console-linux-x64/