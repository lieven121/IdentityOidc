import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/LoginView.vue'),
    },
    {
      path: '/logout',
      name: 'Logout',
      component: () => import('../views/LogoutView.vue'),
    },
    {
      path: '/account',
      name: 'Account',
      component: () => import('../views/AccountView.vue'),
    },
  ],
})

export default router
