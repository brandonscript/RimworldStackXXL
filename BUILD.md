# StackXXL Mod - Build Instructions

This document explains how to build and test the StackXXL mod for RimWorld on macOS.

## Prerequisites

1. **.NET SDK**: Install from [Microsoft's website](https://dotnet.microsoft.com/download)
2. **RimWorld**: Installed on your Mac (Steam or standalone)
3. **HugsLib**: Required dependency for the mod
4. **Required DLLs**: Copy the following files to the `Libraries/` folder:
   - `Assembly-CSharp.dll` (from RimWorld installation)
   - `HugsLib.dll` (from HugsLib mod)
   - `0Harmony.dll` (from HugsLib mod or Harmony mod)
   - `UnityEngine.dll` (from RimWorld installation)

## Quick Build

Use the automated build script:

```bash
# Build only
./build.sh

# Build and install to RimWorld automatically
./build.sh --install
```

## Manual Build Process

If you prefer to build manually:

```bash
# Clean previous builds
rm -rf bin obj

# Build the project
dotnet build StackXXL.csproj --configuration Release

# Copy assembly to mod structure
cp bin/Release/StackXXL.dll Resources/v1.5/Assemblies/
cp bin/Release/StackXXL.dll Resources/v1.6/Assemblies/
```

## Installation

### Automatic Installation

The build script can automatically find and install to your RimWorld mods directory:

```bash
./build.sh --install
```

### Manual Installation

1. Copy the entire `Resources` folder to your RimWorld mods directory
2. Rename it to `StackXXL`
3. Ensure HugsLib is installed and enabled

### RimWorld Mods Directory Locations (macOS)

- `~/Library/Application Support/RimWorld/Mods/`
- `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
- `/Applications/RimWorld.app/Contents/Resources/Data/Mods/`

## Testing the Mod

1. **Install HugsLib** first (required dependency)
   - Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=818773962
2. **Enable the mod** in RimWorld:

   - Launch RimWorld
   - Go to Mods menu
   - Enable "HugsLib" first
   - Enable "StackXXL"
   - Restart RimWorld

3. **Configure settings**:

   - In-game: Options → Mod Settings → StackXXL
   - Adjust stack multipliers as desired
   - **Important**: Restart the game after changing settings

4. **Test functionality**:
   - Start a new game or load an existing save
   - Check that item stack sizes have changed according to your settings
   - Enable debug mode in mod settings to see detailed logs

## Performance Features

This version includes several performance optimizations:

- **Selective Updates**: Only processes changed categories
- **Advanced Caching**: Reduces repeated calculations
- **Optimized Algorithms**: Faster loading times
- **Memory Efficiency**: Reduced garbage collection

## Troubleshooting

### Build Issues

- Ensure .NET SDK is installed: `dotnet --version`
- Check that all reference DLLs are in the `Libraries` folder
- **Missing 0Harmony.dll**: Copy `0Harmony.dll` from your HugsLib mod installation to `Libraries/`
- Clean build: `rm -rf bin obj` then rebuild

### Runtime Issues

- Verify HugsLib is installed and loaded before StackXXL
- Check RimWorld logs for error messages
- Enable debug mode in mod settings for detailed logging
- Restart RimWorld after changing mod settings

### Harmony/HugsLib Dependency Errors

If you see errors like `ReflectionTypeLoadException` or `Could not resolve type with token 0100009b (from typeref, class/assembly HarmonyLib.CodeInstruction)`:

1. **Ensure 0Harmony.dll is present**: Copy `0Harmony.dll` to the `Libraries/` folder
2. **Version mismatch**: Make sure you're using compatible versions of HugsLib and Harmony
3. **Rebuild the mod**: Run `./build.sh` after adding the missing DLL
4. **Check HugsLib installation**: Ensure HugsLib is properly installed and working in RimWorld

### Mod Not Loading

- Confirm mod is in the correct directory structure
- Check that `About.xml` is present and valid
- Ensure assembly is in the correct version folder (`v1.5/Assemblies/` or `v1.6/Assemblies/`)

## Development

### Project Structure

```
RimworldStackXXL/
├── build.sh              # Build script
├── StackXXL.csproj       # Project file
├── StackXXLMod.cs        # Main mod code
├── Libraries/            # RimWorld reference DLLs
└── Resources/            # Mod content
    ├── About/            # Mod metadata
    ├── Languages/        # Translations
    ├── v1.5/Assemblies/  # Compiled mod DLL for RimWorld 1.5
    └── v1.6/Assemblies/  # Compiled mod DLL for RimWorld 1.6+
```

### Making Changes

1. Edit `StackXXLMod.cs`
2. Run `./build.sh --install` to build and install
3. Restart RimWorld to test changes

## Version Compatibility

This mod supports RimWorld versions:

- 1.5, 1.6+

The build script copies the assembly to all version folders for maximum compatibility.

