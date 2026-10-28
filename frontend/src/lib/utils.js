import { clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs) {
  return twMerge(clsx(inputs))
}

/**
 * Resolves an image URL so that local Telegram images always route through
 * the Nginx reverse proxy cache (e.g. /api/images/...) regardless of how they were stored.
 */
export function resolveImageUrl(url) {
  if (!url || typeof url !== 'string') return ''
  const idx = url.indexOf('/api/images/')
  if (idx !== -1) {
    return url.substring(idx)
  }
  return url
}
