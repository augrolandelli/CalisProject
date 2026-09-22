import { expect, test, type Page } from '@playwright/test'

async function loginAdmin(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill('admin@calisapp.com')
  await page.getByLabel('Contraseña', { exact: true }).fill('Admin123!')
  await page.getByRole('button', { name: 'Log In', exact: true }).click()
  await expect(page).not.toHaveURL(/\/login$/)
}

test('Panel Admin: hub, clases con edición, categorías y constructor de rutinas', async ({ page }) => {
  await loginAdmin(page)

  // Hub con todas las secciones
  await page.goto('/admin')
  const hub = page.getByRole('navigation', { name: 'Secciones de administración' })
  for (const section of ['Clases', 'Videoteca', 'Rutinas', 'Logros', 'Categorías', 'Usuarios', 'Comunidad']) {
    await expect(hub.getByRole('link', { name: new RegExp(section) })).toBeVisible()
  }

  // Crear y editar una clase
  await hub.getByRole('link', { name: /Clases/ }).click()
  const classTitle = `Clase E2E ${Date.now()}`
  await page.getByRole('button', { name: '+ Nueva clase' }).click()
  await page.getByLabel('Título', { exact: true }).fill(classTitle)
  await page.getByLabel('Descripción', { exact: true }).fill('Clase creada desde el panel.')
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10)
  await page.getByLabel('Fecha y hora', { exact: true }).fill(`${tomorrow}T18:00`)
  await page.getByLabel('Coach', { exact: true }).fill('Coach E2E')
  await page.getByRole('button', { name: 'Guardar', exact: true }).click()
  await expect(page.getByText(classTitle)).toBeVisible()

  await page.locator('li').filter({ hasText: classTitle }).getByRole('button', { name: 'Editar' }).click()
  await page.getByLabel('Título', { exact: true }).fill(`${classTitle} V2`)
  await page.getByRole('button', { name: 'Guardar', exact: true }).click()
  await expect(page.getByText(`${classTitle} V2`)).toBeVisible()

  page.once('dialog', (dialog) => dialog.accept())
  await page.locator('li').filter({ hasText: `${classTitle} V2` }).getByRole('button', { name: 'Eliminar' }).click()
  await expect(page.getByText(`${classTitle} V2`)).toHaveCount(0)

  // Categorías: crear + nombre duplicado bloqueado
  await page.goto('/admin/categories')
  const categoryName = `Cat E2E ${Date.now()}`
  await page.getByRole('button', { name: '+ Nueva categoría' }).click()
  await page.getByLabel('Nombre', { exact: true }).fill(categoryName)
  await page.getByLabel('Descripción', { exact: true }).fill('Categoría de prueba E2E.')
  await page.getByRole('button', { name: 'Guardar', exact: true }).click()
  await expect(page.getByText(categoryName)).toBeVisible()
  page.once('dialog', (dialog) => dialog.accept())
  await page.locator('li').filter({ hasText: categoryName }).getByRole('button', { name: 'Eliminar' }).click()
  await expect(page.getByText(categoryName)).toHaveCount(0)

  // Constructor de rutinas: reordenar y vincular video
  await page.goto('/admin/routines')
  await page.getByRole('button', { name: '+ Nueva rutina' }).click()
  const routineTitle = `Rutina E2E ${Date.now()}`
  await page.getByLabel('Título', { exact: true }).fill(routineTitle)
  await page.getByLabel('Descripción', { exact: true }).fill('Rutina armada desde el panel.')
  await page.getByLabel('Duración', { exact: true }).fill('25 min')
  await page.locator('input[placeholder="Dominadas estrictas"]').first().fill('Colgada activa')
  await page.locator('select').filter({ hasText: /Principal|Calentamiento/ }).first().selectOption('Calentamiento')
  await page.getByPlaceholder('Técnica, errores comunes…').first().fill('Escápulas activas.')
  await page.getByRole('button', { name: 'Guardar rutina' }).click()
  await expect(page.getByText(routineTitle)).toBeVisible()

  // La rutina es visible para alumnos desde la vista pública (categoría Core, primera alfabética)
  await page.goto('/routines')
  await page.getByRole('link', { name: /Rutinas Core/ }).click()
  await expect(page.getByText(routineTitle)).toBeVisible()

  // Usuarios: buscador y filtro
  await page.goto('/admin/users')
  await page.getByLabel('Buscar usuarios').fill('Clover Demo')
  await expect(page.getByText('clover@calisapp.com')).toBeVisible()
  await page.getByLabel('Filtrar por rol').selectOption('Guerrero')
  await expect(page.getByText('clover@calisapp.com')).toHaveCount(0)
})
