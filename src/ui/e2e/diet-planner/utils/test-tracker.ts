import fs from 'fs';
import path from 'path';

const TRACKER_PATH = path.resolve('playwright/.test-data.json');

interface TrackedData {
  recipes: string[];
  products: string[];
}

function readTracker(): TrackedData {
  if (!fs.existsSync(TRACKER_PATH)) {
    return { recipes: [], products: [] };
  }
  try {
    return JSON.parse(fs.readFileSync(TRACKER_PATH, 'utf-8'));
  } catch {
    return { recipes: [], products: [] };
  }
}

function writeTracker(data: TrackedData) {
  const dir = path.dirname(TRACKER_PATH);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(TRACKER_PATH, JSON.stringify(data, null, 2));
}

export function trackCreatedId(type: 'recipes' | 'products', id: string) {
  const data = readTracker();
  if (!data[type].includes(id)) {
    data[type].push(id);
    writeTracker(data);
  }
}

export function getTrackedIds(): TrackedData {
  return readTracker();
}

export function clearTracker() {
  if (fs.existsSync(TRACKER_PATH)) {
    fs.unlinkSync(TRACKER_PATH);
  }
}
