#!/bin/bash

# RimWorld StackXXL Mod Build Script

echo "Building StackXXL mod..."
cd RimworldStackXXL

# Build the mod
xbuild StackXXL.csproj /p:Configuration=Release

if [ $? -eq 0 ]; then
  echo "✅ Build successful!"
  echo "📦 Output: bin/Release/StackXXL.dll"
  ls -la bin/Release/StackXXL.dll
else
  echo "❌ Build failed!"
  exit 1
fi
