#!/usr/bin/env bash
# Instala e roda o app no tablet/emulador conectado (via adb). Uso: ./run.sh
set -e
cd "$(dirname "$0")"
source ./env.sh
echo "Dispositivos conectados:"
adb devices
dotnet build SeminarioMobile/SeminarioMobile.csproj -f net9.0-android -t:Run \
    -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" -p:JavaSdkDirectory="$JAVA_HOME" "$@"
