// Helpers for converting between user-local "HH:mm" form values and the
// backend's UTC `TimeOnly` representation (`"HH:mm:ss"`). The weekly-summary
// schedule is stored as a (DayOfWeek, TimeOfDay) pair in UTC; if the local
// time-of-day rolls over midnight when shifted to UTC, the day-of-week shifts
// with it.
//
// REASON: backend operates entirely in UTC per design spec §2; v1 accepts
// DST drift on stored wall-clock preferences.

const REFERENCE_DATE = new Date(2026, 0, 5); // a Monday — stable for time-only conversions

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}

export function localTimeToUtc(localHHmm: string): string {
  const [h, m] = localHHmm.split(':').map(Number);
  const d = new Date(REFERENCE_DATE);
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return `${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}:00`;
}

export function utcTimeToLocal(utcHHmmss: string): string {
  const [h, m] = utcHHmmss.split(':').map(Number);
  const d = new Date(REFERENCE_DATE);
  d.setUTCHours(h ?? 0, m ?? 0, 0, 0);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// Shifts a (local DayOfWeek, local HH:mm) pair to UTC. If converting the local
// time crosses midnight in either direction, the day-of-week shifts accordingly.
export function localDayAndTimeToUtc(
  localDayOfWeek: number,
  localHHmm: string,
): { dayOfWeekUtc: number; timeOfDayUtc: string } {
  const [h, m] = localHHmm.split(':').map(Number);
  const sundayBase = new Date(2026, 0, 4); // a Sunday
  const d = new Date(sundayBase);
  d.setDate(sundayBase.getDate() + localDayOfWeek);
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return {
    dayOfWeekUtc: d.getUTCDay(),
    timeOfDayUtc: `${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}:00`,
  };
}

export function utcDayAndTimeToLocal(
  dayOfWeekUtc: number,
  timeOfDayUtc: string,
): { localDayOfWeek: number; localHHmm: string } {
  const [h, m] = timeOfDayUtc.split(':').map(Number);
  const sundayBase = new Date(Date.UTC(2026, 0, 4));
  const d = new Date(sundayBase);
  d.setUTCDate(sundayBase.getUTCDate() + dayOfWeekUtc);
  d.setUTCHours(h ?? 0, m ?? 0, 0, 0);
  return {
    localDayOfWeek: d.getDay(),
    localHHmm: `${pad(d.getHours())}:${pad(d.getMinutes())}`,
  };
}
