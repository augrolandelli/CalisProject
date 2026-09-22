import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../authStore'
import { login } from '../api/authApi'
import { apiErrorMessage } from '../../../shared/api/client'

const loginSchema = z.object({
  email: z.string().min(1, 'Ingresa tu email.').email('Email inválido.'),
  password: z.string().min(1, 'Ingresa tu contraseña.'),
})

type LoginForm = z.infer<typeof loginSchema>

const inputClass =
  'w-full rounded-2xl border border-white/15 bg-surface px-4 py-3.5 text-foreground placeholder:text-muted/60 focus:border-accent focus:outline-none'

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const setSession = useAuthStore((s) => s.setSession)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) })

  const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? '/'

  const onSubmit = async (form: LoginForm) => {
    setServerError(null)
    try {
      const session = await login(form)
      setSession(session)
      navigate(from, { replace: true })
    } catch (error) {
      setServerError(apiErrorMessage(error))
    }
  }

  return (
    <section className="flex min-h-dvh flex-col justify-center px-6 py-10">
      <div className="mx-auto w-full max-w-md">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-5 flex h-20 w-20 items-center justify-center rounded-3xl border border-accent/40 bg-surface text-3xl font-black text-accent">
            C
          </div>
          <h1 className="text-3xl font-black">Valen Team SW</h1>
          <p className="mt-2 text-muted">AUMENTA TU KI</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <div>
            <label htmlFor="email" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              placeholder="email@email.com"
              className={inputClass}
              {...register('email')}
            />
            {errors.email && <p className="mt-1 text-sm text-danger">{errors.email.message}</p>}
          </div>

          <div>
            <label htmlFor="password" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Contraseña
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              className={inputClass}
              {...register('password')}
            />
            {errors.password && <p className="mt-1 text-sm text-danger">{errors.password.message}</p>}
          </div>

          {serverError && (
            <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
              {serverError}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-2xl bg-accent py-4 text-lg font-bold text-accent-foreground shadow-lg shadow-accent/25 transition-opacity disabled:opacity-50"
          >
            {isSubmitting ? 'Ingresando…' : 'Log In'}
          </button>
        </form>

        <p className="mt-8 text-center text-sm text-muted">
          ¿No tienes cuenta?{' '}
          <Link to="/register" className="font-bold text-accent">
            Regístrate
          </Link>
        </p>
      </div>
    </section>
  )
}
