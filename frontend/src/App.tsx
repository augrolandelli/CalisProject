import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { LoadingState } from './shared/components/QueryStates'
import { AppLayout } from './shared/components/AppLayout'
import NotFoundPage from './shared/pages/NotFoundPage'
import { RequireAuth } from './features/auth/components/RequireAuth'
import { RequireRole } from './features/auth/components/RequireRole'
import LoginPage from './features/auth/pages/LoginPage'
import RegisterPage from './features/auth/pages/RegisterPage'
import GuidePage from './features/guide/pages/GuidePage'
import DashboardPage from './features/dashboard/pages/DashboardPage'
import ClassesPage from './features/classes/pages/ClassesPage'
import ClassDetailPage from './features/classes/pages/ClassDetailPage'
import ExercisesPage from './features/exercises/pages/ExercisesPage'
import ExerciseCategoryPage from './features/exercises/pages/ExerciseCategoryPage'
import ExerciseDetailPage from './features/exercises/pages/ExerciseDetailPage'
import RoutinesPage from './features/routines/pages/RoutinesPage'
import RoutineCategoryPage from './features/routines/pages/RoutineCategoryPage'
import RoutineDetailPage from './features/routines/pages/RoutineDetailPage'
import ProfilePage from './features/profile/pages/ProfilePage'
import AchievementsPage from './features/achievements/pages/AchievementsPage'
import AdminUsersPage from './features/admin/pages/AdminUsersPage'
const CommunityPage = lazy(() => import('./features/community/pages/CommunityPage'))
const PostDetailPage = lazy(() => import('./features/community/pages/PostDetailPage'))
const EventDetailPage = lazy(() => import('./features/community/pages/EventDetailPage'))
const AdminCommunityPage = lazy(() => import('./features/admin/pages/AdminCommunityPage'))
const PostEditorPage = lazy(() => import('./features/admin/pages/PostEditorPage'))
const EventEditorPage = lazy(() => import('./features/admin/pages/EventEditorPage'))
const AdminHubPage = lazy(() => import('./features/admin/pages/AdminHubPage'))
const AdminClassesPage = lazy(() => import('./features/admin/pages/AdminClassesPage'))
const AdminVideosPage = lazy(() => import('./features/admin/pages/AdminVideosPage'))
const AdminRoutinesPage = lazy(() => import('./features/admin/pages/AdminRoutinesPage'))
const AdminAchievementsPage = lazy(() => import('./features/admin/pages/AdminAchievementsPage'))
const AdminCategoriesPage = lazy(() => import('./features/admin/pages/AdminCategoriesPage'))

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<LoadingState />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route element={<RequireAuth />}>
          <Route path="/guia" element={<GuidePage />} />

          <Route element={<AppLayout />}>
            <Route index element={<DashboardPage />} />
            <Route path="classes" element={<ClassesPage />} />
            <Route path="classes/:id" element={<ClassDetailPage />} />
            <Route path="exercises" element={<ExercisesPage />} />
            <Route path="exercises/category/:categoryId" element={<ExerciseCategoryPage />} />
            <Route path="exercises/:id" element={<ExerciseDetailPage />} />
            <Route path="routines" element={<RoutinesPage />} />
            <Route path="routines/category/:categoryId" element={<RoutineCategoryPage />} />
            <Route path="routines/:id" element={<RoutineDetailPage />} />
            <Route path="profile" element={<ProfilePage />} />
            <Route path="achievements" element={<AchievementsPage />} />
            <Route path="community" element={<CommunityPage />} />

            <Route element={<RequireRole roles={['Clover', 'Admin']} />}>
              <Route path="community/posts/:id" element={<PostDetailPage />} />
              <Route path="community/events/:id" element={<EventDetailPage />} />
            </Route>

            <Route element={<RequireRole roles={['Admin']} />}>
              <Route path="admin" element={<AdminHubPage />} />
              <Route path="admin/users" element={<AdminUsersPage />} />
              <Route path="admin/classes" element={<AdminClassesPage />} />
              <Route path="admin/videos" element={<AdminVideosPage />} />
              <Route path="admin/routines" element={<AdminRoutinesPage />} />
              <Route path="admin/achievements" element={<AdminAchievementsPage />} />
              <Route path="admin/categories" element={<AdminCategoriesPage />} />
              <Route path="admin/community" element={<AdminCommunityPage />} />
              <Route path="admin/community/posts/new" element={<PostEditorPage />} />
              <Route path="admin/community/posts/:id/edit" element={<PostEditorPage />} />
              <Route path="admin/community/events/new" element={<EventEditorPage />} />
              <Route path="admin/community/events/:id/edit" element={<EventEditorPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
