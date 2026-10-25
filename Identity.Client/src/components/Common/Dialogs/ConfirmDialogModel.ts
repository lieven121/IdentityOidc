export interface ConfirmDialogModel {
  title: string
  message: string
  confirm: ButtonOptions
  cancel: ButtonOptions
  /** When set, the dialog asks for a value and closes with it instead of `true`. */
  input?: InputOptions
}

export interface InputOptions {
  label: string
  placeholder?: string
  autocomplete?: string
  type?: 'text' | 'password'
}

export interface ButtonOptions {
  text: string
  severity: 'primary' | 'secondary' | 'danger' | 'success' | 'info' | 'warning'
}
