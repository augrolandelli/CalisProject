import { apiClient } from '../../../shared/api/client'
import type { SessionDto } from '../../classes/api/classesApi'
import type { CategoryDto } from '../../../shared/types/catalog'
import type { VideoDto } from '../../exercises/api/exercisesApi'

// ---- Usuarios ----

export interface AdminUserDto {
  id: number
  fullName: string
  email: string
  role: 'Guerrero' | 'Clover' | 'Admin'
  state: string
}

export async function getUsers(filters: { search?: string; role?: string } = {}): Promise<AdminUserDto[]> {
  const { data } = await apiClient.get<AdminUserDto[]>('/user', { params: filters })
  return data
}

export async function changeRole(userId: number, role: string): Promise<AdminUserDto> {
  const { data } = await apiClient.patch<AdminUserDto>(`/user/${userId}/role`, { role })
  return data
}

// ---- Clases ----

export interface WriteSessionRequest {
  title: string
  description: string
  date: string
  limitedSpots: number
  difficulty: string
  coachName: string
  durationMinutes: number
  achievementIds: number[]
}

export const getAllSessions = async () =>
  (await apiClient.get<SessionDto[]>('/session')).data

export const createSession = async (request: WriteSessionRequest) =>
  (await apiClient.post<SessionDto>('/session', request)).data

export const updateSession = async (id: number, request: WriteSessionRequest) =>
  (await apiClient.put<SessionDto>(`/session/${id}`, request)).data

export const deleteSession = async (id: number) => { await apiClient.delete(`/session/${id}`) }

export const getPurgePreview = async (year: number, month: number) =>
  (await apiClient.get<{ count: number }>('/session/purge-preview', { params: { year, month } })).data

export const purgeSessionsByMonth = async (year: number, month: number) =>
  (await apiClient.delete<{ deleted: number }>('/session', { params: { year, month } })).data

// ---- Categorías ----

export const saveCategory = async (id: number | undefined, name: string, description: string) =>
  (id
    ? await apiClient.put<CategoryDto>(`/category/${id}`, { name, description })
    : await apiClient.post<CategoryDto>('/category', { name, description })).data

export const deleteCategory = async (id: number) => { await apiClient.delete(`/category/${id}`) }

// ---- Videos ----

export interface WriteVideoRequest {
  title: string
  description: string
  difficulty: string
  requisites: string
  categoryId: number
  mediaAssetId: string | null
}

export const saveVideo = async (id: number | undefined, request: WriteVideoRequest) =>
  (id
    ? await apiClient.put<VideoDto>(`/video/${id}`, request)
    : await apiClient.post<VideoDto>('/video', request)).data

export const deleteVideo = async (id: number) => { await apiClient.delete(`/video/${id}`) }

// ---- Rutinas ----

export interface WriteRutineExercise {
  exercise: string
  tipo: 'Calentamiento' | 'Principal'
  reps: number
  series: number
  descanso: string
  obs: string
  videoId: number | null
}

export interface WriteRutineRequest {
  title: string
  description: string
  duration: string
  difficulty: string
  categoryId: number
  exercises: WriteRutineExercise[]
}

export interface RutineDetail {
  id: number
  title: string
  description: string
  duration: string
  difficulty: string
  categoryId: number
  exercises: (WriteRutineExercise & { videoId: number | null })[]
}

export const saveRutine = async (id: number | undefined, request: WriteRutineRequest) =>
  (id
    ? await apiClient.put<RutineDetail>(`/rutine/${id}`, request)
    : await apiClient.post<RutineDetail>('/rutine', request)).data

export const getRutine = async (id: number) =>
  (await apiClient.get<RutineDetail>(`/rutine/${id}`)).data

export const deleteRutine = async (id: number) => { await apiClient.delete(`/rutine/${id}`) }

// ---- Logros ----

export interface AchievementAdminDto {
  id: number
  name: string
  description: string
  icon: string
  isHidden: boolean
  grantedCount: number
}

export const getAdminAchievements = async () =>
  (await apiClient.get<AchievementAdminDto[]>('/achievement/admin')).data

export const saveAchievement = async (id: number | undefined, name: string, description: string, icon: string) =>
  (id
    ? await apiClient.put(`/achievement/${id}`, { name, description, icon })
    : await apiClient.post('/achievement', { name, description, icon })).data

export const deleteAchievement = async (id: number) => { await apiClient.delete(`/achievement/${id}`) }
