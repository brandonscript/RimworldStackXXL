#!/bin/bash

# Quick development testing script
# Builds and installs the mod, then provides helpful testing info

set -e

echo "🔨 Quick Dev Build & Install..."

# Build and install
./build.sh --install

echo ""
echo "🎮 Testing Instructions:"
echo "1. Launch RimWorld"
echo "2. Go to Mods → Enable HugsLib (if not already enabled)"
echo "3. Enable StackXXL mod"
echo "4. Restart RimWorld"
echo "5. Options → Mod Settings → StackXXL to configure"
echo ""
echo "🔍 Debug Tips:"
echo "- Enable 'Debug Mode' in mod settings for detailed logs"
echo "- Check stack sizes in-game: wood, steel, silver, etc."
echo "- Test different multiplier settings (10x, 20x, etc.)"
echo ""
echo "📋 Default Settings to Try:"
echo "- XL Size: 10x"
echo "- XXL Size: 20x"
echo "- Resources: XL (10x)"
echo "- Silver: XL (10x)"
echo "- Meals: XL (10x)"
echo ""
echo "✅ Ready to test! Remember to restart RimWorld after changing settings."
