export type LocationEnum = 'West Region' | 'North Region' | 'North-East Region' | 'East Region' | 'Central Region';

export type EmissionType = 'Food' | 'Transport' | 'Utilities';

export interface LeaderboardEntry {
  rank: number;
  username: string;
  nickname: string;
  emissions: number;
  avatarUrl: string;
}

export interface User {
  id: string;
  name: string;
  nickname: string;
  email: string;
  location: LocationEnum;
  birthDate: string;
  avatar: string;
  joinDays: number;
}

export interface Record {
  id: string;
  date: string;
  type: EmissionType;
  amount: number;
  unit: string;
  description: string;
}
