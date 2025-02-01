import './assets/main.css'

import { createApp } from 'vue'

import Pinia from './utils/pinia'
import PrimeVueUtil from './utils/primevue'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(Pinia)
app.use(PrimeVueUtil)
app.use(router)

app.mount('#app')
