import { apiClient } from '../../../shared/api/client'
import type { UserDto } from '../../auth/types'

export interface UserStatsDto {
  achievementsCount: number
  upcomingClasses: number
  attendedClasses: number
  missedClasses: number
  classesThisMonth: number
}

export interface UpdateProfileRequest {
  fullName: string
  phone: string
  photoUrl: string | null
}

export async function getMe(): Promise<UserDto> {
  const { data } = await apiClient.get<UserDto>('/me')
  return data
}

export async function updateMe(request: UpdateProfileRequest): Promise<UserDto> {
  const { data } = await apiClient.patch<UserDto>('/me', request)
  return data
}

export async function getMyStats(): Promise<UserStatsDto> {
  const { data } = await apiClient.get<UserStatsDto>('/me/stats')
  return data
}
