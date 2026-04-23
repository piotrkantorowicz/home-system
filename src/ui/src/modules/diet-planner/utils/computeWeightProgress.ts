export type WeightProgressDirection = 'losing' | 'gaining' | 'maintaining';

export interface WeightProgressInput {
  start: number;
  current: number;
  target: number;
}

export interface WeightProgressResult {
  kgRemaining: number;
  percentComplete: number;
  direction: WeightProgressDirection;
}

const MAINTAINING_THRESHOLD_KG = 0.5;

export function computeWeightProgress({
  start,
  current,
  target,
}: WeightProgressInput): WeightProgressResult {
  const totalDelta = target - start;
  const achievedDelta = current - start;

  let direction: WeightProgressDirection;
  if (Math.abs(achievedDelta) < MAINTAINING_THRESHOLD_KG) {
    direction = 'maintaining';
  } else if (achievedDelta < 0) {
    direction = 'losing';
  } else {
    direction = 'gaining';
  }

  let percentComplete: number;
  if (totalDelta === 0) {
    percentComplete = 100;
  } else {
    const ratio = achievedDelta / totalDelta;
    percentComplete = Math.max(0, Math.min(100, ratio * 100));
  }

  // If target reached or surpassed (in the right direction), nothing remains.
  const targetReached = totalDelta === 0 || achievedDelta / totalDelta >= 1;
  const kgRemaining = targetReached ? 0 : Math.abs(target - current);

  return { kgRemaining, percentComplete, direction };
}
