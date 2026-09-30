#!/usr/bin/env bash
# Ambiente do toolchain MAUI/Android para este projeto (SDK oficial da Microsoft,
# não o dotnet do Fedora, que não tem os workloads mobile).
# Uso: source env.sh
export DOTNET_ROOT=/home/broonoh/.dotnet-maui
export JAVA_HOME=/home/broonoh/.jdks/jdk-21.0.12.1+1
export ANDROID_SDK_ROOT=/home/broonoh/Android/Sdk
export ANDROID_HOME=/home/broonoh/Android/Sdk
export PATH=/home/broonoh/.dotnet-maui:$JAVA_HOME/bin:$ANDROID_SDK_ROOT/platform-tools:$PATH
export DOTNET_CLI_TELEMETRY_OPTOUT=1
