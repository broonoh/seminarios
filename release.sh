#!/usr/bin/env bash
# Gera o APK de Release assinado, pronto para instalar fora da Play Store.
set -e
cd "$(dirname "$0")"
source ./env.sh

dotnet publish SeminarioMobile/SeminarioMobile.csproj -f net9.0-android -c Release \
    -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" -p:JavaSdkDirectory="$JAVA_HOME" \
    -p:AndroidSigningKeyStore="$(pwd)/seminario-release.keystore" \
    -p:AndroidSigningKeyAlias=seminario \
    -p:AndroidSigningKeyPass='Seminario2026!' \
    -p:AndroidSigningStorePass='Seminario2026!' \
    -p:AndroidPackageFormat=apk

echo ""
echo "APK gerado em:"
find SeminarioMobile/bin/Release/net9.0-android -iname "*-Signed.apk"
