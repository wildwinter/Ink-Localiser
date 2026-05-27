#!/bin/bash

rm -rf ./publish/*

version="$1"
targets=("osx-arm64" "osx-x64" "win-x86" "win-x64")

for target in "${targets[@]}"; do

    cd LocaliserTool
    dotnet publish -c Release -r ${target} -o ../publish/${target}
    cd ..

    rm ./publish/${target}/*.pdb
    # Combine project license with Ink's license (Ink DLLs are embedded in the self-contained executable)
    { cat ./LICENSE; printf '\n---\n\nThis distribution includes components from Ink (https://github.com/inkle/ink):\n\n'; cat ./Lib/Inklecate/LICENSE; } > ./publish/${target}/LICENSE
    cp ./README.md ./publish/${target}
    cp -r ./docs ./publish/${target}

    if [[ "${target}" == osx-* ]]; then
        codesign --sign "${APPLE_CODESIGN_ID}" --timestamp --options runtime --force ./publish/${target}/LocaliserTool
    fi

    cd ./publish/${target}
    zip -r "../LocaliserTool-${target}-${version}".zip .
    cd ../..

done

mkdir ./publish/dll
cp ./LocaliserLib/bin/Release/net8.0/LocaliserLib.dll ./publish/dll
cp ./LICENSE ./publish/dll

cd ./publish/dll
zip -r "../LocaliserLib-${version}.zip" .
cd ../..