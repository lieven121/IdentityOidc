import { useUserStore } from '@/stores/user'
import { createRouter, createWebHistory } from 'vue-router'
import { RouteNames } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: RouteNames.Root,
      redirect: '/account',
    },
    {
      path: '/login',
      name: RouteNames.Login,
      component: () => import('../views/LoginView.vue'),
    },
    {
      path: '/logout',
      name: RouteNames.Logout,
      component: () => import('../views/LogoutView.vue'),
    },
    {
      path: '/account',
      name: RouteNames.Account,
      component: () => import('../views/AccountView.vue'),
      beforeEnter: (to, from, next) => {
        const userStore = useUserStore()
        if (userStore.isAuthenticated) next()
        else next('/login?ReturnUrl=' + encodeURIComponent(to.fullPath))
      },
      children: [
        {
          path: '',
          name: RouteNames.AccountDefault,
          redirect: '/account/info',
        },
        {
          path: 'info',
          name: RouteNames.AccountInfo,
          component: () => import('../components/Account/AccountInfo.vue'),
        },
        {
          path: 'security',
          name: RouteNames.Security,
          component: () => import('../components/Account/Security/SecuritySettings.vue'),
        },
        {
          path: 'admin/users',
          name: RouteNames.AdminUsers,
          component: () => import('../components/Account/Admin/UsersManagement.vue'),
          meta: { requiresAdmin: true },
        },
        {
          path: 'admin/roles',
          name: RouteNames.AdminRoles,
          component: () => import('../components/Account/Admin/RolesManagement.vue'),
          meta: { requiresAdmin: true },
        },
        {
          path: 'admin/applications',
          name: RouteNames.AdminApplications,
          component: () => import('../components/Account/Admin/ApplicationsManagement.vue'),
          meta: { requiresAdmin: true },
        },
      ],
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  if (window.location.hostname === 'localhost' && window.location.port === '5173') {
    window.location.href = 'https://localhost:7038' + to.fullPath
    return
  }
  const userStore = useUserStore()
  await userStore.loadUser()
  // Users who must use 2FA can only reach the security page until it is set up.
  const exempt: (string | symbol | undefined | null)[] = [RouteNames.Security, RouteNames.Logout, RouteNames.Login]
  if (userStore.user?.requiresTwoFactorSetup && !exempt.includes(to.name)) {
    next({
      name: RouteNames.Security,
      query: { setup: 'required', ...(to.query.ReturnUrl ? { ReturnUrl: to.query.ReturnUrl } : {}) },
    })
  } else if (to.meta.requiresAdmin && !userStore.isAdmin) {
    next({ name: RouteNames.AccountInfo })
  } else {
    next()
  }
})

export default router
