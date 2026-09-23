export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface User {
  id: string;
  userName: string;
  email: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}