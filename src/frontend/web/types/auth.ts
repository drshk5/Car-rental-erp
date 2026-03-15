export type AuthUser = {
  id: string;
  fullName: string;
  email: string;
  role: string | number;
  branchId: string;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: AuthUser;
};

export type UserSession = {
  accessToken: string;
  userId: string;
  fullName: string;
  email: string;
  role: string;
  branchId: string;
  expiresAtUtc: string | null;
};
