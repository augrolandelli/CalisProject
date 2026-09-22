import { apiClient } from '../../../shared/api/client'

export interface AchievementDto {
  id: number
  name: string
  description: string
  icon: string
}

export interface UserAchievementDto extends AchievementDto {
  achievementId: number
  dateEarned: string
  sessionId: number | null
}

export interface GrantAchievementResponse {
  granted: number
  skipped: number
}

export async function getAchievements(): Promise<AchievementDto[]> {
  const { data } = await apiClient.get<AchievementDto[]>('/achievement')
  return data
}

export async function getMyAchievements(): Promise<UserAchievementDto[]> {
  const { data } = await apiClient.get<UserAchievementDto[]>('/me/achievements')
  return data
}

export async function getSessionAchievements(sessionId: number): Promise<AchievementDto[]> {
  const { data } = await apiClient.get<AchievementDto[]>(`/session/${sessionId}/achievements`)
  return data
}

export async function grantAchievement(
  achievementId: number,
  sessionId: number,
  userIds: number[],
): Promise<GrantAchievementResponse> {
  const { data } = await apiClient.post<GrantAchievementResponse>('/achievement/grant', {
    achievementId,
    sessionId,
    userIds,
  })
  return data
}
