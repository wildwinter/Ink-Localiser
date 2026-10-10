// Sync the root package.json version into the .csproj files, so the version in
// source matches the one that gets published. Runs as part of `npm run build`,
// which the publish scripts invoke first, so publishing picks it up too.

import { readFileSync, writeFileSync } from 'fs';

const root = JSON.parse(readFileSync(new URL('../package.json', import.meta.url)));
const version = root.version;

for (const rel of ['../LocaliserLib/LocaliserLib.csproj', '../LocaliserTool/LocaliserTool.csproj']) {
    const path = new URL(rel, import.meta.url);
    const before = readFileSync(path, 'utf8');
    const after = before.replace(/<Version>.*?<\/Version>/, `<Version>${version}</Version>`);
    if (after !== before) {
        writeFileSync(path, after);
    }
}

console.log(`Versions synced to ${version}`);
