#!/bin/bash

# CoreApiClient Build Script
# This script builds, tests, and packages the CoreApiClient library

set -e  # Exit on error

echo "========================================="
echo "CoreApiClient Build Script"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK is not installed${NC}"
    echo "Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download"
    exit 1
fi

# Display .NET version
echo -e "${GREEN}✓ .NET SDK found${NC}"
dotnet --version
echo ""

# Step 1: Clean
echo "Step 1: Cleaning previous builds..."
dotnet clean -c Release > /dev/null 2>&1 || true
rm -rf src/CoreApiClient/bin/Release/*.nupkg 2> /dev/null || true
echo -e "${GREEN}✓ Clean completed${NC}"
echo ""

# Step 2: Restore
echo "Step 2: Restoring NuGet packages..."
if dotnet restore; then
    echo -e "${GREEN}✓ Restore completed${NC}"
else
    echo -e "${RED}✗ Restore failed${NC}"
    exit 1
fi
echo ""

# Step 3: Build
echo "Step 3: Building solution..."
if dotnet build -c Release --no-restore; then
    echo -e "${GREEN}✓ Build completed${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi
echo ""

# Step 4: Run Tests
echo "Step 4: Running tests..."
if dotnet test --no-build -c Release --verbosity quiet; then
    echo -e "${GREEN}✓ All tests passed${NC}"
else
    echo -e "${RED}✗ Tests failed${NC}"
    exit 1
fi
echo ""

# Step 5: Pack
echo "Step 5: Creating NuGet package..."
if dotnet pack src/CoreApiClient/CoreApiClient.csproj -c Release --no-build -o ./artifacts; then
    echo -e "${GREEN}✓ Package created${NC}"
else
    echo -e "${RED}✗ Packaging failed${NC}"
    exit 1
fi
echo ""

# Step 6: Display package info
echo "Step 6: Package information..."
PACKAGE_FILE=$(find ./artifacts -name "CoreApiClient.*.nupkg" -not -name "*.symbols.nupkg" | head -n 1)
if [ -f "$PACKAGE_FILE" ]; then
    echo -e "${GREEN}✓ Package location: $PACKAGE_FILE${NC}"
    ls -lh "$PACKAGE_FILE"
    
    # Display package size
    SIZE=$(du -h "$PACKAGE_FILE" | cut -f1)
    echo -e "  Package size: ${YELLOW}$SIZE${NC}"
else
    echo -e "${RED}✗ Package file not found${NC}"
fi
echo ""

# Step 7: Run examples (optional)
if [ "$1" == "--run-examples" ]; then
    echo "Step 7: Running examples..."
    cd examples/CoreApiClient.Examples
    if dotnet run --no-build -c Release; then
        echo -e "${GREEN}✓ Examples completed${NC}"
    else
        echo -e "${YELLOW}⚠ Examples failed or were interrupted${NC}"
    fi
    cd ../..
    echo ""
fi

echo "========================================="
echo -e "${GREEN}Build completed successfully!${NC}"
echo "========================================="
echo ""
echo "Next steps:"
echo "  1. Review the package: unzip -l $PACKAGE_FILE"
echo "  2. Test locally: dotnet add package $PACKAGE_FILE"
echo "  3. Publish to NuGet: dotnet nuget push $PACKAGE_FILE --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
echo ""
