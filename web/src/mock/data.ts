import type { LeaderboardEntry } from '../types';

// Shared mock leaderboard data source (single source of truth)
export const mockLeaderboardData: LeaderboardEntry[] = [
  {
    rank: 1,
    username: 'Melody',
    nickname: 'EcoRanger',
    emissions: 1.20,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Melody',
  },
  {
    rank: 2,
    username: 'Aisha',
    nickname: 'GreenGuru',
    emissions: 1.31,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Aisha',
  },
  {
    rank: 3,
    username: 'Wei Ming',
    nickname: 'EcoRanger',
    emissions: 1.46,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=WeiMing',
  },
  {
    rank: 4,
    username: 'Arjun',
    nickname: 'LeafLover',
    emissions: 1.58,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Arjun',
  },
  {
    rank: 5,
    username: 'Hana',
    nickname: 'CarbonNinja',
    emissions: 1.66,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Hana',
  },
  {
    rank: 6,
    username: 'Nina',
    nickname: 'GreenGuru',
    emissions: 1.81,
    avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Nina',
  },
];

export function getTopLeaderboard(n = 5) {
  return mockLeaderboardData.slice(0, n);
}

export function updateLeaderboardAvatar(username: string, avatarUrl: string) {
  const entry = mockLeaderboardData.find((x) => x.username === username);
  if (entry) entry.avatarUrl = avatarUrl;
}

export function updateLeaderboardNickname(username: string, nickname: string) {
  const entry = mockLeaderboardData.find((x) => x.username === username);
  if (entry) entry.nickname = nickname;
}

