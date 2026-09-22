/**
 * Contacto de administración para el upgrade manual Guerrero → Clover.
 * Flujo definido por el cliente: el interesado escribe por WhatsApp y un
 * admin le cambia el rol desde /admin/users.
 * TODO: reemplazar por el número real del gimnasio.
 */
export const ADMIN_WHATSAPP_URL =
  'https://wa.me/5492323211719?text=' +
  encodeURIComponent('Hola! Quiero empezar a ser un clover.')
