dotnet --info

echo The installed .NET Core SDKs are:
dir $env:ProgramFiles"\dotnet\sdk" | findstr /l "."

dotnet restore IdentityServer4.Armut.RedisStore.sln

dotnet build ./IdentityServer4.Armut.RedisStore/IdentityServer4.Armut.RedisStore.csproj -c Release