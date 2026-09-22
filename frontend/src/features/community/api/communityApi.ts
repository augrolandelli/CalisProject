import axios from 'axios'
import { apiClient } from '../../../shared/api/client'

export interface Page<T> { items: T[]; total: number; page: number; pageSize: number }
export interface Media { id: string; fileName: string; contentType: string; size: number }
export interface Post {
  id: number; title: string; content: string; kind: 'announcement' | 'achievement' | 'competition'
  createdAt: string; updatedAt: string | null; authorName: string; startsAt: string | null
  location: string | null; likeCount: number; isLiked: boolean; media: Media[]
}
export interface WritePost {
  title: string; content: string; kind: 'announcement' | 'competition'
  startsAt: string | null; location: string | null; mediaIds: string[]
}
export interface Review {
  id: number; userId: number; authorName: string; sessionId: number | null; sessionTitle: string
  sessionDate: string; rating: number; content: string | null; createdAt: string; isHidden: boolean
}
export interface ReviewPage { reviews: Page<Review>; averageRating: number | null; ratingCount: number }
export interface ReviewEligibility { canReview: boolean; reason: string | null; myReview: Review | null }
export interface CommunityEvent {
  id: number; title: string; description: string; location: string; startsAt: string; endsAt: string
  capacity: number | null; registeredCount: number; isRegistered: boolean; isCancelled: boolean
}
export type WriteEvent = Pick<CommunityEvent, 'title' | 'description' | 'location' | 'startsAt' | 'endsAt' | 'capacity'>
export interface EventParticipant { userId: number; fullName: string; registeredAt: string }

export const getPosts = async (page = 1, signal?: AbortSignal) =>
  (await apiClient.get<Page<Post>>('/post', { params: { page }, signal })).data
export const getPost = async (id: number, signal?: AbortSignal) =>
  (await apiClient.get<Post>(`/post/${id}`, { signal })).data
export const savePost = async (id: number | undefined, request: WritePost) =>
  (id ? await apiClient.put<Post>(`/post/${id}`, request) : await apiClient.post<Post>('/post', request)).data
export const deletePost = async (id: number) => { await apiClient.delete(`/post/${id}`) }
export const setLike = async (id: number, liked: boolean) => {
  if (liked) await apiClient.put(`/post/${id}/like`)
  else await apiClient.delete(`/post/${id}/like`)
}
export const getMediaLink = async (id: string, signal?: AbortSignal) =>
  (await apiClient.get<{ url: string; expiresAt: string }>(`/media/${id}/link`, { signal })).data
export const abandonMedia = async (id: string) => { await apiClient.delete(`/media/${id}`) }

/** El PUT firmado va directo al almacenamiento: no usa el cliente con JWT de la aplicación. */
export async function uploadMedia(
  file: File,
  onProgress: (value: number) => void,
  signal: AbortSignal,
  purpose: 'community' | 'exercise' = 'community',
): Promise<Media> {
  const { data: upload } = await apiClient.post<{ id: string; uploadUrl: string }>('/media', {
    fileName: file.name, contentType: file.type, size: file.size, purpose,
  }, { signal })
  try {
    await axios.put(upload.uploadUrl, file, {
      headers: { 'Content-Type': file.type }, signal, timeout: 180000,
      onUploadProgress: (event) => onProgress(Math.round((event.loaded / (event.total || file.size)) * 85)),
    })
    onProgress(90)
    const { data } = await apiClient.post<Media>(`/media/${upload.id}/complete`, null, { signal, timeout: 180000 })
    onProgress(100)
    return data
  } catch (error) {
    void abandonMedia(upload.id).catch(() => {})
    throw error
  }
}

export const getReviews = async (page: number, sessionId?: number, moderation = false, signal?: AbortSignal) =>
  (await apiClient.get<ReviewPage>(moderation ? '/review/moderation' : '/review', {
    params: { page, pageSize: sessionId ? 5 : 20, sessionId }, signal,
  })).data
export const getReviewEligibility = async (sessionId: number, signal?: AbortSignal) =>
  (await apiClient.get<ReviewEligibility>(`/review/session/${sessionId}/mine`, { signal })).data
export const saveReview = async (sessionId: number, rating: number, content: string) =>
  (await apiClient.put<Review>(`/review/session/${sessionId}`, { rating, content })).data
export const deleteReview = async (id: number) => { await apiClient.delete(`/review/${id}`) }
export const moderateReview = async (id: number, isHidden: boolean) => {
  await apiClient.put(`/review/${id}/visibility`, { isHidden })
}
export const getEvents = async (page: number, history = false, signal?: AbortSignal) =>
  (await apiClient.get<Page<CommunityEvent>>('/event', { params: { page, history }, signal })).data
export const getEvent = async (id: number, signal?: AbortSignal) =>
  (await apiClient.get<CommunityEvent>(`/event/${id}`, { signal })).data
export const saveEvent = async (id: number | undefined, request: WriteEvent) =>
  (id ? await apiClient.put<CommunityEvent>(`/event/${id}`, request) : await apiClient.post<CommunityEvent>('/event', request)).data
export const cancelEvent = async (id: number) => { await apiClient.post(`/event/${id}/cancel`) }
export const setEventRegistration = async (id: number, registered: boolean) => {
  if (registered) await apiClient.put(`/event/${id}/registration`)
  else await apiClient.delete(`/event/${id}/registration`)
}
export const getEventParticipants = async (id: number, page: number, signal?: AbortSignal) =>
  (await apiClient.get<Page<EventParticipant>>(`/event/${id}/participants`, { params: { page }, signal })).data
