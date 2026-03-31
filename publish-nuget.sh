#!/bin/bash
# Publish LocaliserLib to NuGet.
#
# Usage:
#   ./publish-nuget.sh <version>
#
# Requires:
#   NUGET_API_KEY  environment variable set to your nuget.org API key.
#
# Example:
#   NUGET_API_KEY=xxxxxxxx ./publish-nuget.sh 1.0.0

set -e

VERSION="$1"
if [ -z "$VERSION" ]; then
    echo "Error: version argument required."
    echo "Usage: publish-nuget.sh <version>"
    echo "e.g.   publish-nuget.sh 1.0.0"
    exit 1
fi

if [ -z "$NUGET_API_KEY" ]; then
    echo "Error: NUGET_API_KEY environment variable is not set."
    exit 1
fi

OUTDIR="./nuget-out"
rm -rf "$OUTDIR"

echo "Packing wildwinter.LocaliserLib v${VERSION}..."
cd LocaliserLib
dotnet pack -c Release -p:Version="${VERSION}" -o "../${OUTDIR}"
cd ..

PACKAGE="${OUTDIR}/wildwinter.LocaliserLib.${VERSION}.nupkg"
if [ ! -f "$PACKAGE" ]; then
    echo "Error: expected package not found at ${PACKAGE}"
    exit 1
fi

echo "Pushing to nuget.org..."
dotnet nuget push "$PACKAGE" \
    --api-key "$NUGET_API_KEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

echo "Done. Package published: wildwinter.LocaliserLib ${VERSION}"
