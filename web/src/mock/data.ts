import type { LeaderboardEntry } from '../types';

function hashString(input: string) {
  let hash = 0;
  for (let i = 0; i < input.length; i += 1) {
    hash = (hash * 31 + input.charCodeAt(i)) >>> 0;
  }
  return hash;
}

function makePoints(username: string) {
  // Deterministic "random" points per user, with:
  // pointsTotal >= pointsMonth >= pointsWeek
  const h = hashString(username);
  const pointsWeek = 30 + (h % 170); // 30..199
  const pointsMonth = pointsWeek + 120 + ((h >>> 8) % 380); // +120..+499
  const pointsTotal = pointsMonth + 600 + ((h >>> 16) % 2400); // +600..+2999
  return { pointsWeek, pointsMonth, pointsTotal };
}

// Shared mock leaderboard data source (single source of truth)
export const mockLeaderboardData: LeaderboardEntry[] = [
  {
    rank: 1,
    username: 'Melody',
    nickname: 'EcoRanger',
    emissions: 1.20,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Melody',
    ...makePoints('Melody'),
  },
  {
    rank: 2,
    username: 'Aisha',
    nickname: 'GreenGuru',
    emissions: 1.31,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Aisha',
    ...makePoints('Aisha'),
  },
  {
    rank: 3,
    username: 'Wei Ming',
    nickname: 'EcoRanger',
    emissions: 1.46,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=WeiMing',
    ...makePoints('Wei Ming'),
  },
  {
    rank: 4,
    username: 'Arjun',
    nickname: 'LeafLover',
    emissions: 1.58,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Arjun',
    ...makePoints('Arjun'),
  },
  {
    rank: 5,
    username: 'Hana',
    nickname: 'CarbonNinja',
    emissions: 1.66,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Hana',
    ...makePoints('Hana'),
  },
  {
    rank: 6,
    username: 'Nina',
    nickname: 'GreenGuru',
    emissions: 1.81,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Nina',
    ...makePoints('Nina'),
  },
];

export function getTopLeaderboard(n = 5) {
  return mockLeaderboardData.slice(0, n);
}

export function getLeaderboardUser(username: string) {
  return mockLeaderboardData.find((x) => x.username === username);
}

export function updateLeaderboardAvatar(username: string, avatarUrl: string) {
  const entry = mockLeaderboardData.find((x) => x.username === username);
  if (entry) entry.avatarUrl = avatarUrl;
}

export function updateLeaderboardNickname(username: string, nickname: string) {
  const entry = mockLeaderboardData.find((x) => x.username === username);
  if (entry) entry.nickname = nickname;
}
