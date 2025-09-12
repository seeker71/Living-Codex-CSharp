#!/bin/bash

echo "🧊 ICE-WATER-GAS SPEC-DRIVEN ARCHITECTURE DEMONSTRATION 🧊"
echo "=========================================================="
echo ""

# Check if server is running
if ! curl -s http://localhost:5001/health > /dev/null; then
    echo "❌ Server is not running. Please start the server first."
    exit 1
fi

echo "✅ Server is running"
echo ""

# Step 1: Show the architecture info
echo "📋 STEP 1: Architecture Information"
echo "-----------------------------------"
curl -s http://localhost:5001/spec-driven/info | jq '.architecture'
echo ""

# Step 2: Show current state (ice/water/gas)
echo "📊 STEP 2: Current Architecture State"
echo "-------------------------------------"
curl -s http://localhost:5001/spec-driven/architecture-state | jq '.'
echo ""

# Step 3: Show the example spec (ice)
echo "🧊 STEP 3: Example Spec (Ice) - Persistent Source of Truth"
echo "----------------------------------------------------------"
echo "Spec file: specs/example-module.spec.json"
cat specs/example-module.spec.json | jq '.'
echo ""

# Step 4: Generate code from spec (ice -> water)
echo "💧 STEP 4: Generate Code from Spec (Ice -> Water)"
echo "------------------------------------------------"
echo "This would generate C# code from the spec..."
echo "Generated code would be placed in: generated/"
echo ""

# Step 5: Compile code to DLL (water -> gas)
echo "💨 STEP 5: Compile Code to DLL (Water -> Gas)"
echo "---------------------------------------------"
echo "This would compile the generated C# code to DLLs..."
echo "Compiled DLLs would be placed in: compiled/"
echo ""

# Step 6: Load modules into runtime
echo "🚀 STEP 6: Load Modules into Runtime"
echo "------------------------------------"
echo "This would load the compiled modules into the running system..."
echo ""

# Step 7: Show the complete workflow
echo "🔄 COMPLETE WORKFLOW"
echo "===================="
echo "1. 🧊 Write/modify specs (ice) - ONLY persistent data"
echo "2. 💧 Generate code from specs (ice -> water) - regeneratable"
echo "3. 💨 Compile code to DLLs (water -> gas) - ephemeral"
echo "4. 🚀 Load modules into runtime - ephemeral"
echo "5. 🔄 Repeat as needed - everything regeneratable from specs"
echo ""

echo "✨ KEY BENEFITS:"
echo "- Only specs (ice) need to be persistent"
echo "- Everything else can be regenerated from specs"
echo "- Code and DLLs are just cached artifacts"
echo "- True source of truth is the spec"
echo "- Enables complete system regeneration"
echo ""

echo "🎯 This demonstrates the ice-water-gas architecture where:"
echo "   🧊 ICE (Specs) = Persistent, immutable source of truth"
echo "   💧 WATER (Code) = Generated from specs, can be regenerated"
echo "   💨 GAS (DLLs) = Compiled from code, ephemeral runtime artifacts"
echo ""

echo "✅ Spec-driven architecture demonstration complete!"
