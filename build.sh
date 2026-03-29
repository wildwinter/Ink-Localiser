#!/bin/bash

rm -rf ./publish/*

version="0.0.4.0"
targets=("osx-arm64" "osx-x64" "win-x86" "win-x64")

for target in "${targets[@]}"; do

    cd LocaliserTool
    dotnet publish -c Release -r ${target} -o ../publish/${target}
    cd ..

    rm ./publish/${target}/*.pdb
    cp ./LICENSE ./publish/${target}
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
# SimpleVCLib is a dependency of LocaliserLib — include it for library users.
if [ -f "./Lib/simple-vc-lib/SimpleVCLib.dll" ]; then
    cp ./Lib/simple-vc-lib/SimpleVCLib.dll ./publish/dll
fi
cp ./LICENSE ./publish/dll

cd ./publish/dll
zip -r "../LocaliserLib-${version}.zip" .
cd ../..