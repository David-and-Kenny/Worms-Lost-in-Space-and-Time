import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const project = path.join(root, 'Worms Lost in Space and Time(3.0)');
const assets = path.join(project, 'Assets');
const errors = [];
const guids = new Map();
let assetCount = 0;
function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const file = path.join(dir, entry.name);
    if (entry.name.endsWith('.meta')) {
      if (!fs.existsSync(file.slice(0, -5))) errors.push(`Orphan metadata: ${file}`);
      const guid = fs.readFileSync(file, 'utf8').match(/^guid: ([a-f0-9]{32})$/m)?.[1];
      if (!guid) errors.push(`Invalid metadata GUID: ${file}`);
      else if (guids.has(guid)) errors.push(`Duplicate GUID: ${file} and ${guids.get(guid)}`);
      else guids.set(guid, file);
      continue;
    }
    if (entry.name.startsWith('.')) continue;
    assetCount++;
    if (!fs.existsSync(`${file}.meta`)) errors.push(`Missing metadata: ${file}`);
    if (entry.isDirectory()) walk(file);
    else if (/\.(cs|unity|prefab|asset|mat|anim|controller)$/.test(entry.name)) {
      if (/^(<{7}|={7}|>{7})( |$)/m.test(fs.readFileSync(file, 'utf8'))) {
        errors.push(`Unresolved merge conflict: ${file}`);
      }
    }
  }
}
walk(assets);
const baselinePath = path.join(root, 'docs/asset-guid-baseline.json');
if (fs.existsSync(baselinePath)) {
  for (const original of JSON.parse(fs.readFileSync(baselinePath, 'utf8'))) {
    if (!guids.has(original.guid)) errors.push(`Original asset GUID lost: ${original.path} (${original.guid})`);
  }
}
const settings = fs.readFileSync(path.join(project, 'ProjectSettings/EditorSettings.asset'), 'utf8');
if (!/m_SerializationMode: 2\b/.test(settings)) errors.push('Enable Force Text serialization.');
const versionSettings = path.join(project, 'ProjectSettings/VersionControlSettings.asset');
const versionControl = fs.existsSync(versionSettings) ? fs.readFileSync(versionSettings, 'utf8') : settings;
if (!/Visible Meta Files/.test(versionControl)) errors.push('Enable Visible Meta Files.');
const builds = fs.readFileSync(path.join(project, 'ProjectSettings/EditorBuildSettings.asset'), 'utf8');
let scenes = 0;
for (const match of builds.matchAll(/- enabled: 1\s+path: (.+)/g)) {
  scenes++;
  if (!fs.existsSync(path.join(project, match[1].trim()))) errors.push(`Missing build scene: ${match[1]}`);
}
if (!scenes) errors.push('No enabled build scenes.');
const tracked = execFileSync('git', ['ls-files', '-z'], { cwd: root, encoding: 'utf8' }).split('\0');
for (const file of tracked) {
  if (/(^|\/)(Library|Temp|Obj|Logs|UserSettings|Build|Builds)\//i.test(file) || /\.(apk|aab|keystore|jks|ulf)$/i.test(file)) {
    errors.push(`Generated output or private file tracked by Git: ${file}`);
  }
}
if (errors.length) {
  console.error(errors.join('\n'));
  process.exitCode = 1;
} else console.log(`Repository checks passed: ${assetCount} assets/folders, ${guids.size} GUIDs, ${scenes} build scenes.`);
