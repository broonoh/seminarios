#!/usr/bin/env bash
# Builda o app para Android. Uso: ./build.sh
set -e
cd "$(dirname "$0")"
source ./env.sh
dotnet build SeminarioMobile/SeminarioMobile.csproj -f net9.0-android \
    -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" -p:JavaSdkDirectory="$JAVA_HOME" "$@"
