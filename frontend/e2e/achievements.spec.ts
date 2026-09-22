import { expect, test, type Page } from '@playwright/test'

async function loginClover(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill('clover@calisapp.com')
  await page.getByLabel('Contraseña', { exact: true }).fill('Clover1234!')
  await page.getByRole('button', { name: 'Log In', exact: true }).click()
  await expect(page).not.toHaveURL(/\/login$/)
}

async function loginAdmin(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill('admin@calisapp.com')
  await page.getByLabel('Contraseña', { exact: true }).fill('Admin123!')
  await page.getByRole('button', { name: 'Log In', exact: true }).click()
  await expect(page).not.toHaveURL(/\/login$/)
}

test('Logros: el catálogo marca los obtenidos y el admin puede asociarlos a una clase', async ({ page }) => {
  // 1. El Clover demo tiene el logro "Primera clase" sembrado (UserAchievement sin clase)
  await loginClover(page)
  await page.goto('/achievements')
  await expect(page.getByText('Primera clase', { exact: true })).toBeVisible()
  const earnedCard = page.locator('li').filter({ hasText: 'Primera clase' })
  await expect(earnedCard).toContainText('OBTENIDO')

  // Otro logro del catálogo debe aparecer bloqueado
  await expect(page.getByText('First Muscle-Up', { exact: true })).toBeVisible()
  const lockedCard = page.locator('li').filter({ hasText: 'First Muscle-Up' })
  await expect(lockedCard).toContainText('🔒')

  // 2. Admin asocia un logro nuevo a una clase futura
  await loginAdmin(page)
  await page.goto('/admin/achievements')
  const achievementName = `Logro E2E ${Date.now()}`
  await page.getByRole('button', { name: '+ Nuevo logro' }).click()
  await page.getByLabel('Nombre', { exact: true }).fill(achievementName)
  await page.getByLabel('Descripción', { exact: true }).fill('Logro de prueba E2E.')
  await page.getByLabel(/Icono/).fill('🏆')
  await page.getByRole('button', { name: 'Guardar', exact: true }).click()
  await expect(page.getByText(achievementName)).toBeVisible()

  await page.goto('/admin/classes')
  const classTitle = `Clase Logros E2E ${Date.now()}`
  await page.getByRole('button', { name: '+ Nueva clase' }).click()
  await page.getByLabel('Título', { exact: true }).fill(classTitle)
  await page.getByLabel('Descripción', { exact: true }).fill('Clase para probar asociación de logros.')
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10)
  await page.getByLabel('Fecha y hora', { exact: true }).fill(`${tomorrow}T18:00`)
  await page.getByLabel('Coach', { exact: true }).fill('Coach E2E')

  const achievementCheckbox = page.locator('label').filter({ hasText: achievementName }).locator('input[type="checkbox"]')
  await achievementCheckbox.check()
  await page.getByRole('button', { name: 'Guardar', exact: true }).click()
  await expect(page.getByText(classTitle)).toBeVisible()

  // Al volver a editar, el logro debe seguir seleccionado
  await page.locator('li').filter({ hasText: classTitle }).getByRole('button', { name: 'Editar' }).click()
  await expect(achievementCheckbox).toBeChecked()
})
