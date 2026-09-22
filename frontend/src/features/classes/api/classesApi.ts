import { apiClient } from '../../../shared/api/client'

export interface SessionDto {
  id: number
  title: string
  description: string
  date: string
  limitedSpots: number
  enrolled: number
  availableSpots: number
  difficulty: string
  coachName: string
  durationMinutes: number
}

export interface EnrolledUserDto {
  id: number
  fullName: string
  attended: boolean | null
}

export type SessionUserStatus = 'enrolled' | 'waitlist' | 'none'

export interface SessionDetailsDto {
  session: SessionDto
  enrolledUsers: EnrolledUserDto[]
  currentUserStatus: SessionUserStatus
}

export async function getSessions(datetime?: string): Promise<SessionDto[]> {
  const { data } = await apiClient.get<SessionDto[]>('/session', {
    params: datetime ? { datetime } : {},
  })
  return data
}

export async function getSessionDetails(id: number): Promise<SessionDetailsDto> {
  const { data } = await apiClient.get<SessionDetailsDto>(`/session/${id}/details`)
  return data
}

export async function enroll(sessionId: number): Promise<void> {
  await apiClient.post(`/usersession/${sessionId}`)
}

export async function unenroll(sessionId: number): Promise<void> {
  await apiClient.delete(`/usersession/${sessionId}`)
}

export async function joinWaitlist(sessionId: number): Promise<void> {
  await apiClient.post(`/usersession/${sessionId}/waitlist`)
}

export async function leaveWaitlist(sessionId: number): Promise<void> {
  await apiClient.delete(`/usersession/${sessionId}/waitlist`)
}

export async function recordAttendance(sessionId: number, attendedUserIds: number[]): Promise<void> {
  await apiClient.put(`/session/${sessionId}/attendance`, { attendedUserIds })
}
