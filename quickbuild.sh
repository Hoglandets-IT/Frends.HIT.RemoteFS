#!/usr/bin/bash
VERSION=$(cat ./VERSION)
APIKEY=$1
# Increment version number
IFS=. read -r v1 v2 v3 <<< "$VERSION"
v3=$((v3 + 1))
VERSION="$v1.$v2.$v3"

echo "Building version $VERSION" &&
rm -f ./Frends.HIT.RemoteFS.*.nupkg &&
dotnet pack --configuration Release --include-source --output . /p:Version=$VERSION Frends.HIT.RemoteFS/Frends.HIT.RemoteFS.csproj &&
dotnet nuget push "Frends.HIT.RemoteFS.$VERSION.nupkg" --source https://proget.hoglandet.se/nuget/Frends/ --api-key "$APIKEY" &&
echo "Done" &&
echo "$VERSION" > ./VERSION

rm -f ./Frends.HIT.RemoteFS.*.nupkg