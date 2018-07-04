#!/usr/bin/env bash

# Linux/Darwin
OSNAME=$(uname -s)
echo "OSNAME: $OSNAME"

#exit if any command fails
set -e

export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/

dotnet restore IdentityServer4.Armut.RedisStore.sln

dotnet build ./IdentityServer4.Armut.RedisStore/IdentityServer4.Armut.RedisStore.csproj -c Release