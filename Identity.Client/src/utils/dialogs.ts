import ConfirmDialog from '@/components/Common/Dialogs/ConfirmDialog.vue'
import type { ConfirmDialogModel } from '@/components/Common/Dialogs/ConfirmDialogModel'
import { ApiException } from '@/resources/api-clients/identity-api-client'
import { useDialog } from 'primevue'

const dialogProps = {
  style: { maxWidth: '40rem', width: '100%' },
  modal: true,
  closable: false,
  dismissableMask: false,
  closeOnEscape: false,
  showHeader: false,
}

/** Promise wrappers around the shared ConfirmDialog. Call from setup(). */
export function useConfirmDialog() {
  const dialog = useDialog()

  /** Resolves with the entered value, or null when cancelled. Requires `data.input`. */
  const ask = (data: ConfirmDialogModel): Promise<string | null> =>
    new Promise((resolve) => {
      dialog.open(ConfirmDialog, {
        data,
        props: dialogProps,
        onClose: (opt) => resolve(typeof opt?.data === 'string' ? opt.data : null),
      })
    })

  const confirm = (data: ConfirmDialogModel): Promise<boolean> =>
    new Promise((resolve) => {
      dialog.open(ConfirmDialog, {
        data,
        props: dialogProps,
        onClose: (opt) => resolve(opt?.data === true),
      })
    })

  return { ask, confirm }
}

/**
 * The message from an endpoint that returns a plain string on 400/409. The generated client throws
 * the parsed body itself for typed error responses, and an ApiException otherwise.
 */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'string' && error) return error
  if (error instanceof ApiException && error.response) {
    try {
      const parsed = JSON.parse(error.response)
      if (typeof parsed === 'string') return parsed
    } catch {
      // not a plain message
    }
  }
  return fallback
}
