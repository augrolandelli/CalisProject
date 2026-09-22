export interface UserDto {
  id: number
  fullName: string
  phone: string
  email: string
  role: 'Guerrero' | 'Clover' | 'Admin'
  state: string
  photoUrl: string | null
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserDto
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  fullName: string
  phone: string
  email: string
  password: string
}
