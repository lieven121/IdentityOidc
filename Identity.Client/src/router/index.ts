import { useUserStore } from '@/stores/user'
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/account',
    },
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
      beforeEnter: (to, from, next) => {
        const userStore = useUserStore()
        if (userStore.isAuthenticated) next()
        next('/login?ReturnUrl=' + to.fullPath)
      },
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  const userStore = useUserStore()
  await userStore.loadUser()
  next()
})

export default router
