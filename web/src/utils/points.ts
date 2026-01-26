// Points System – business rules (for backend reference)
// Rule 1: Uploading emission records grants points proportional to reduction effort.
// Rule 2: Top 10 users on the Monthly Leaderboard receive a bonus point award at month-end.

import type { LeaderboardEntry } from '../types';

export type PointsPeriod = 'week' | 'month' | 'all' | 'today';

export function getPoints(entry: LeaderboardEntry, period: PointsPeriod) {
  if (period === 'today') return entry.pointsToday || 0;
  if (period === 'week') return entry.pointsWeek;
  if (period === 'month') return entry.pointsMonth;
  return entry.pointsTotal;
}

