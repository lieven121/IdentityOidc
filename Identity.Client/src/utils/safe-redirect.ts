/**
 * Returns the ReturnUrl query parameter as a same-origin path, or null.
 * Only same-origin targets are allowed: blocks open redirects and javascript: URLs.
 */
export function getSafeReturnUrl(): string | null {
  const returnUrl = new URLSearchParams(window.location.search).get('ReturnUrl')
  if (!returnUrl) return null

  try {
    const target = new URL(returnUrl, window.location.origin)
    if (target.origin !== window.location.origin) return null
    return target.pathname + target.search + target.hash
  } catch {
    return null
  }
}

/**
 * Navigates to the ReturnUrl with a full page load (it may be a server endpoint such as
 * /connect/authorize). Returns false when there is no safe ReturnUrl, so the caller can fall back.
 */
export function followReturnUrl(): boolean {
  const target = getSafeReturnUrl()
  if (!target) return false
  window.location.href = target
  return true
}
