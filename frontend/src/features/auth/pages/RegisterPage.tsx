import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../authStore'
import { register as registerUser } from '../api/authApi'
import { apiErrorMessage } from '../../../shared/api/client'

const registerSchema = z.object({
  fullName: z.string().min(2, 'Ingresa tu nombre completo.').max(200),
  phone: z.string().min(6, 'Ingresa un teléfono válido.').max(50),
  email: z.string().min(1, 'Ingresa tu email.').email('Email inválido.'),
  password: z.string().min(8, 'Mínimo 8 caracteres.').max(100),
})

type RegisterForm = z.infer<typeof registerSchema>

const inputClass =
  'w-full rounded-2xl border border-white/15 bg-surface px-4 py-3.5 text-foreground placeholder:text-muted/60 focus:border-accent focus:outline-none'

export default function RegisterPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({ resolver: zodResolver(registerSchema) })

  const onSubmit = async (form: RegisterForm) => {
    setServerError(null)
    try {
      const session = await registerUser(form)
      setSession(session)
      navigate('/', { replace: true })
    } catch (error) {
      setServerError(apiErrorMessage(error))
    }
  }

  return (
    <section className="flex min-h-dvh flex-col justify-center px-6 py-10">
      <div className="mx-auto w-full max-w-md">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-black">Crea tu cuenta</h1>
          <p className="mt-2 text-muted">Empeza como Guerrero</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <div>
            <label htmlFor="fullName" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Nombre completo
            </label>
            <input id="fullName" autoComplete="name" placeholder="Tu nombre" className={inputClass} {...register('fullName')} />
            {errors.fullName && <p className="mt-1 text-sm text-danger">{errors.fullName.message}</p>}
          </div>

          <div>
            <label htmlFor="phone" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Teléfono
            </label>
            <input id="phone" type="tel" autoComplete="tel" placeholder="9 2323 212529" className={inputClass} {...register('phone')} />
            {errors.phone && <p className="mt-1 text-sm text-danger">{errors.phone.message}</p>}
          </div>

          <div>
            <label htmlFor="email" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Email
            </label>
            <input id="email" type="email" autoComplete="email" placeholder="email@email.com" className={inputClass} {...register('email')} />
            {errors.email && <p className="mt-1 text-sm text-danger">{errors.email.message}</p>}
          </div>

          <div>
            <label htmlFor="password" className="mb-1.5 block text-xs font-bold tracking-wider uppercase">
              Contraseña
            </label>
            <input id="password" type="password" autoComplete="new-password" placeholder="Mínimo 8 caracteres" className={inputClass} {...register('password')} />
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
            {isSubmitting ? 'Creando cuenta…' : 'Sign Up'}
          </button>
        </form>

        <p className="mt-8 text-center text-sm text-muted">
          ¿Ya tienes cuenta?{' '}
          <Link to="/login" className="font-bold text-accent">
            Inicia sesión
          </Link>
        </p>
      </div>
    </section>
  )
}
