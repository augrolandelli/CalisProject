import { apiClient } from '../../../shared/api/client'

export interface RutineSummaryDto {
  id: number
  title: string
  description: string
  duration: string
  difficulty: string
  categoryName: string
}

export interface RutineExerciseDto {
  exercise: string
  tipo: 'Calentamiento' | 'Principal' | string
  reps: number
  series: number
  descanso: string
  obs: string
  videoId: number | null
}

export interface RutineDetailDto {
  id: number
  title: string
  description: string
  duration: string
  difficulty: string
  categoryId: number
  exercises: RutineExerciseDto[]
}

export interface RutineFilters {
  categoryId?: number
  searchTerm?: string
}

export async function getRutines(filters: RutineFilters = {}): Promise<RutineSummaryDto[]> {
  const { data } = await apiClient.get<RutineSummaryDto[]>('/rutine', { params: filters })
  return data
}

export async function getRutine(id: number): Promise<RutineDetailDto> {
  const { data } = await apiClient.get<RutineDetailDto>(`/rutine/${id}`)
  return data
}
