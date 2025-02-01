import type { App, Plugin } from 'vue'
import { createPinia } from 'pinia'

const Pinia: Plugin = {
  install: (app: App) => {
    const pina = createPinia()
    app.use(pina)
  },
}

export default Pinia
