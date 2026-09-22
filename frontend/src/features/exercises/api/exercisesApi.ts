import { apiClient } from '../../../shared/api/client'
import type { CategoryDto } from '../../../shared/types/catalog'

export interface VideoDto {
  id: number
  title: string
  description: string
  difficulty: string
  requisites: string
  url: string
  categoryId: number
  category: CategoryDto
}

export interface VideoFilters {
  categoryId?: number
  searchTerm?: string
}

export async function getCategories(): Promise<CategoryDto[]> {
  const { data } = await apiClient.get<CategoryDto[]>('/category')
  return data
}

export async function getVideos(filters: VideoFilters = {}): Promise<VideoDto[]> {
  const { data } = await apiClient.get<VideoDto[]>('/video', { params: filters })
  return data
}

export async function getVideo(id: number): Promise<VideoDto> {
  const { data } = await apiClient.get<VideoDto>(`/video/${id}`)
  return data
}
