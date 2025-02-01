import type { App, Plugin } from 'vue'

import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import DialogService from 'primevue/dialogservice'
import PrimeVue from 'primevue/config'
import BadgeDirective from 'primevue/badgedirective'
import Tooltip from 'primevue/tooltip'
import Message from 'primevue/message'

import Aura from '@primevue/themes/aura'
import { definePreset } from '@primevue/themes'

const PrimeVueUtil: Plugin = {
  install: (app: App) => {
    //also has options

    const MyPreset = definePreset(Aura, {
      semantic: {
        primary: {
          50: '{blue.50}',
          100: '{blue.100}',
          200: '{blue.200}',
          300: '{blue.300}',
          400: '{blue.400}',
          500: '{blue.500}',
          600: '{blue.600}',
          700: '{blue.700}',
          800: '{blue.800}',
          900: '{blue.900}',
          950: '{blue.950}',
        },
      },
    })

    app.use(PrimeVue, {
      zIndex: {
        modal: 1040,
        overlay: 1030,
        menu: 1020,
        tooltip: 1010,
      },
      theme: {
        preset: MyPreset,
        ripple: true,
        options: {
          darkModeSelector: '.dark-mode',
          cssLayer: {
            name: 'primevue',
            order: 'tailwind-base, primevue, tailwind-utilities',
          },
        },
      },
    })

    app.use(ConfirmationService)
    app.use(ToastService)
    app.use(DialogService)

    app.component('PMessage', Message)

    app.directive('badge', BadgeDirective)
    app.directive('tooltip', Tooltip)
  },
}

export default PrimeVueUtil
