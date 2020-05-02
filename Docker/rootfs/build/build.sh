#!/bin/bash

cd /tmp

echo -e "\033[31mBuilding NetDaemon for platform $TARGETPLATFORM\033[0m" >&2

if [ "$TARGETPLATFORM" == "linux/arm/v7" ]; then
    export RID="linux-arm"
elif [ "$TARGETPLATFORM" == "linux/arm64" ]; then
    export RID="linux-arm"
elif [ "$TARGETPLATFORM" == "linux/amd64" ]; then
    export RID="linux-x64"
else
    echo 'NOT VALID ARCHITECTURE' && exit 1

fi

cd src
dotnet publish -c Release -o /netdaemon/bin/publish -r $RID
