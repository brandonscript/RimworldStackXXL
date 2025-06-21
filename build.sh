#!/bin/bash

# StackXXL Mod Build Script for macOS
# This script builds the mod and optionally installs it to RimWorld

GIT_ROOT=$(git rev-parse --show-toplevel)

set -e # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="StackXXL"
PROJECT_FILE="StackXXL.csproj"
BUILD_CONFIG="Release"
MOD_NAME="StackXXL"
PROJECT_DIR="$GIT_ROOT/RimworldStackXXL"

# RimWorld paths (common locations on macOS)
RIMWORLD_PATHS=(
  "$HOME/Library/Application Support/RimWorld/Mods"
  "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods"
  "/Applications/RimWorld.app/Contents/Resources/Data/Mods"
  "$HOME/.steam/steam/steamapps/common/RimWorld/Mods"
)

echo -e "${BLUE}=== StackXXL Mod Build Script ===${NC}"
echo "Building $PROJECT_NAME..."

# Function to print colored output
print_status() {
  echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
  echo -e "${RED}[ERROR]${NC} $1"
}

# Check if .NET SDK is installed
if ! command -v dotnet &>/dev/null; then
  print_error ".NET SDK is not installed. Please install it from https://dotnet.microsoft.com/download"
  exit 1
fi

print_status ".NET SDK found: $(dotnet --version)"

# Change to project directory
if [ ! -d "$PROJECT_DIR" ]; then
  print_error "Project directory '$(basename $PROJECT_DIR)' not found!"
  exit 1
fi

print_status "Changing to project directory: $PROJECT_DIR"
cd "$PROJECT_DIR"

# Clean previous builds
print_status "Cleaning previous builds..."
if [ -d "bin" ]; then
  rm -rf bin
fi
if [ -d "obj" ]; then
  rm -rf obj
fi

# Build the project
print_status "Building $PROJECT_NAME in $BUILD_CONFIG configuration..."
dotnet build "$PROJECT_FILE" --configuration "$BUILD_CONFIG" --verbosity minimal

if [ $? -ne 0 ]; then
  print_error "Build failed!"
  exit 1
fi

print_status "Build successful!"

# Copy assembly to mod structure
print_status "Copying assembly to mod structure..."

# Ensure assembly directories exist
mkdir -p "Resources/v1.5/Assemblies"
mkdir -p "Resources/v1.6/Assemblies"

# Copy the built assembly to all version folders
cp "bin/$BUILD_CONFIG/$PROJECT_NAME.dll" "Resources/v1.5/Assemblies/"
cp "bin/$BUILD_CONFIG/$PROJECT_NAME.dll" "Resources/v1.6/Assemblies/"

# Update build timestamp in About.xml
print_status "Updating build timestamp..."
BUILD_TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S %Z')

# Simple regex replacement - find existing build timestamp and replace it
if [[ "$OSTYPE" == "darwin"* ]]; then
  # macOS version
  sed -i '' "s/^Build: [0-9][0-9][0-9][0-9].*/Build: $BUILD_TIMESTAMP/" "Resources/About/About.xml"
else
  # Linux version
  sed -i "s/^Build: [0-9][0-9][0-9][0-9].*/Build: $BUILD_TIMESTAMP/" "Resources/About/About.xml"
fi

print_status "Assembly copied to mod structure"

# Function to find RimWorld installation
find_rimworld() {
  for path in "${RIMWORLD_PATHS[@]}"; do
    if [ -d "$path" ]; then
      echo "$path"
      return 0
    fi
  done
  return 1
}

# Check if user wants to install the mod
if [ "$1" = "--install" ] || [ "$1" = "-i" ]; then
  print_status "Attempting to install mod to RimWorld..."

  RIMWORLD_MODS_DIR=$(find_rimworld)

  if [ $? -eq 0 ]; then
    print_status "Found RimWorld mods directory: $RIMWORLD_MODS_DIR"

    MOD_INSTALL_DIR="$RIMWORLD_MODS_DIR/$MOD_NAME"

    # Remove existing installation
    if [ -d "$MOD_INSTALL_DIR" ]; then
      print_warning "Removing existing mod installation..."
      rm -rf "$MOD_INSTALL_DIR"
    fi

    # Create mod directory
    mkdir -p "$MOD_INSTALL_DIR"

    # Copy mod files
    print_status "Installing mod files..."
    cp -r Resources/* "$MOD_INSTALL_DIR/"

    print_status "Mod installed successfully to: $MOD_INSTALL_DIR"
    print_status "You can now enable '$MOD_NAME' in RimWorld's mod manager"

    # Check for HugsLib dependency
    HUGSLIB_DIR="$RIMWORLD_MODS_DIR/HugsLib"
    if [ ! -d "$HUGSLIB_DIR" ]; then
      print_warning "HugsLib not found in mods directory!"
      print_warning "Make sure to install HugsLib from the Steam Workshop or manually"
      print_warning "Steam Workshop URL: https://steamcommunity.com/sharedfiles/filedetails/?id=818773962"
    else
      print_status "HugsLib dependency found"
    fi

  else
    print_error "Could not find RimWorld mods directory!"
    print_error "Please manually copy the 'Resources' folder contents to your RimWorld mods directory"
    print_error "Common locations:"
    for path in "${RIMWORLD_PATHS[@]}"; do
      print_error "  - $path"
    done
    exit 1
  fi
else
  print_status "Build complete! Mod files are ready in the 'Resources' directory"
  print_status "To automatically install to RimWorld, run: ./build.sh --install"
  print_status ""
  print_status "Manual installation:"
  print_status "1. Copy the 'Resources' folder contents to your RimWorld mods directory"
  print_status "2. Rename the copied folder to '$MOD_NAME'"
  print_status "3. Make sure HugsLib is installed and enabled"
  print_status "4. Enable '$MOD_NAME' in RimWorld's mod manager"
fi

# Display build summary
echo ""
echo -e "${GREEN}=== Build Summary ===${NC}"
echo "Project: $PROJECT_NAME"
echo "Configuration: $BUILD_CONFIG"
echo "Assembly: $PROJECT_DIR/bin/$BUILD_CONFIG/$PROJECT_NAME.dll"
echo "Mod ready: $PROJECT_DIR/Resources/"
echo ""
echo -e "${BLUE}Usage:${NC}"
echo "  ./build.sh          - Build only"
echo "  ./build.sh --install - Build and install to RimWorld"
echo "  ./build.sh -i        - Build and install to RimWorld (short form)"
echo ""
print_status "Done!"
