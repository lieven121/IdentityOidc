export interface ConfirmDialogModel {
  title: string
  message: string
  confirm: ButtonOptions
  cancel: ButtonOptions
}

export interface ButtonOptions {
  text: string
  severity: 'primary' | 'secondary' | 'danger' | 'success' | 'info' | 'warning'
}
