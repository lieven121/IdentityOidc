<script setup lang="ts">
import { RouteNames } from '@/router/routes'

interface MenuItem {
  label: string
  icon: string
  routeName: string
  adminOnly?: boolean
  dividerBefore?: boolean
}

const userStore = useUserStore()
const router = useRouter()
const route = useRoute()

const menuItems = computed<MenuItem[]>(() => {
  const items: MenuItem[] = [
    { label: 'Account Info', icon: 'fa-duotone fa-user', routeName: RouteNames.AccountInfo },
    { label: 'Security', icon: 'fa-duotone fa-shield', routeName: RouteNames.Security },
    { label: 'Logout', icon: 'fa-duotone fa-right-from-bracket', routeName: RouteNames.Logout },
  ]

  // Add admin section if user is admin
  if (userStore.isAdmin) {
    items.push(
      { label: 'Users', icon: 'fa-duotone fa-users', routeName: RouteNames.AdminUsers, adminOnly: true, dividerBefore: true },
      { label: 'Roles', icon: 'fa-duotone fa-key', routeName: RouteNames.AdminRoles, adminOnly: true },
      { label: 'Applications', icon: 'fa-duotone fa-box', routeName: RouteNames.AdminApplications, adminOnly: true }
    )
  }

  return items
})

const isMobile = ref(false)
// const showMobileMenu = ref(true)

const showMobileMenu = computed({
  // getter
  get() {
    if (!isMobile.value) return false
    return route.query['show'] !== 'true'
  },
  // setter
  set(newValue) {
    if (!isMobile.value) {
      router.replace({ query: { ...route.query, show: undefined } })
      return
    }
    router.replace({ query: { ...route.query, show: !newValue ? 'true' : undefined }, force: true })
  }
})

const checkMobile = () => {
  isMobile.value = window.innerWidth < 768
}

const handlePopState = () => {
  // When user navigates back on mobile, show the menu
  if (isMobile.value && !showMobileMenu.value) {
    showMobileMenu.value = true
  }
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
  window.addEventListener('popstate', handlePopState)
})

onUnmounted(() => {
  window.removeEventListener('resize', checkMobile)
  window.removeEventListener('popstate', handlePopState)
})

const handleMenuItemClick = async (item: MenuItem) => {
  // Navigate to the route
  await router.push({ name: item.routeName })

  // On mobile, hide menu after navigation to show content
  if (isMobile.value) {
    showMobileMenu.value = false
  }
}

const goBackToMenu = () => {
  showMobileMenu.value = true
}

// Watch route changes to update mobile menu visibility
watch(() => route.name, () => {
  if (isMobile.value && route.name !== RouteNames.Account) {
    showMobileMenu.value = false
  }
})
</script>

<template>
  <div class="account-layout">
    <div
      class="background"
      id="AccountBackground"
    ></div>
    <div
      class="layout-container"
      id="AccountLayoutContainer"
    >
      <!-- Mobile: Show either menu or content -->
      <template v-if="isMobile">
        <!-- Mobile Menu List -->
        <div
          v-if="showMobileMenu"
          class="mobile-menu"
        >
          <Card>
            <template #header>
              <div class="menu-header">
                <Avatar
                  :label="userStore.user?.username?.[0]?.toUpperCase()"
                  size="xlarge"
                  shape="circle"
                  class="user-avatar"
                />
                <div class="user-info">
                  <h2>{{ userStore.user?.username }}</h2>
                  <p>{{ userStore.user?.email }}</p>
                </div>
              </div>
            </template>
            <template #content>
              <div class="menu-list">
                <template
                  v-for="item in menuItems"
                  :key="item.routeName"
                >
                  <Divider v-if="item.dividerBefore" />
                  <div
                    class="menu-item"
                    :class="{ 'admin-item': item.adminOnly }"
                    @click="handleMenuItemClick(item)"
                  >
                    <i :class="item.icon"></i>
                    <span>{{ item.label }}</span>
                    <i class="fa-duotone fa-chevron-right"></i>
                  </div>
                </template>
              </div>
            </template>
          </Card>
        </div>

        <!-- Mobile Content View -->
        <div
          v-else
          class="mobile-content"
        >
          <div class="mobile-header">
            <Button
              icon="fa-duotone fa-arrow-left"
              text
              @click="goBackToMenu"
              aria-label="Back to menu"
            />
            <h2>{{menuItems.find(item => route.name === item.routeName)?.label}}</h2>
          </div>
          <div class="content-wrapper">
            <slot></slot>
          </div>
        </div>
      </template>

      <!-- Desktop: Sidebar + Content -->
      <template v-else>
        <div class="desktop-layout">
          <!-- Sidebar Navigation -->
          <aside class="sidebar">
            <Card>
              <template #header>
                <div class="menu-header">
                  <Avatar
                    :label="userStore.user?.username?.[0]?.toUpperCase()"
                    size="xlarge"
                    shape="circle"
                    class="user-avatar"
                  />
                  <div class="user-info">
                    <h2>{{ userStore.user?.username }}</h2>
                    <p>{{ userStore.user?.email }}</p>
                  </div>
                </div>
              </template>
              <template #content>
                <nav class="sidebar-menu">
                  <template
                    v-for="item in menuItems"
                    :key="item.routeName"
                  >
                    <Divider v-if="item.dividerBefore" />
                    <router-link
                      :to="{ name: item.routeName }"
                      class="menu-link"
                      :class="{ 'admin-item': item.adminOnly, 'active': route.name === item.routeName }"
                    >
                      <i :class="item.icon"></i>
                      <span>{{ item.label }}</span>
                    </router-link>
                  </template>
                </nav>
              </template>
            </Card>
          </aside>

          <!-- Main Content Area -->
          <main class="content">
            <slot></slot>
          </main>
        </div>
      </template>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.account-layout {
  min-height: 100vh;
  position: relative;
}

.background {
  background-image: url('/images/bg.jpg');
  background-repeat: no-repeat;
  background-size: cover;
  background-position: right;
  background-position-x: 80%;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: -1;
}

.layout-container {
  padding: 1rem;
  max-width: 1400px;
  margin: 0 auto;
}

// Common styles
.menu-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  text-align: center;

  .user-avatar {
    background: var(--primary-color);
  }

  .user-info {
    h2 {
      margin: 0;
      font-size: 1.5rem;
    }

    p {
      margin: 0.25rem 0 0 0;
      color: var(--text-color-secondary);
      font-size: 0.9rem;
    }
  }
}

// Mobile Styles
.mobile-menu {
  .menu-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .menu-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    border-radius: var(--border-radius);
    cursor: pointer;
    transition: background-color 0.2s;

    &:hover {
      background: var(--surface-hover);
    }

    i:first-child {
      font-size: 1.25rem;
      width: 1.5rem;
    }

    span {
      flex: 1;
      font-size: 1rem;
    }

    i:last-child {
      font-size: 0.875rem;
      color: var(--text-color-secondary);
    }

    &.admin-item {
      i:first-child {
        color: var(--orange-500);
      }
    }
  }
}

.mobile-content {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 2rem);

  .mobile-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1rem;
    background: var(--surface-card);
    padding: 0.5rem;
    border-radius: var(--border-radius);
    box-shadow: var(--card-shadow);
    flex-shrink: 0;

    h2 {
      margin: 0;
      font-size: 1.25rem;
    }
  }

  .content-wrapper {
    animation: slideIn 0.3s ease-out;
    flex: 1;
    overflow: hidden;
    min-height: 0;
  }
}

// Desktop Styles
.desktop-layout {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 2rem;
  align-items: start;
}

.sidebar {
  position: sticky;
  top: 1rem;

  .sidebar-menu {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;

    .menu-link {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.875rem 1rem;
      border-radius: var(--border-radius);
      text-decoration: none;
      color: var(--text-color);
      transition: all 0.2s;

      &:hover {
        background: var(--surface-hover);
      }

      &.active {
        background: var(--primary-color);
        color: var(--primary-color-text);
      }

      i {
        font-size: 1.25rem;
        width: 1.5rem;
      }

      span {
        font-size: 1rem;
      }

      &.admin-item:not(.active) {
        i {
          color: var(--orange-500);
        }
      }
    }
  }
}

.content {
  height: calc(100vh - 2rem);
  animation: fadeIn 0.3s ease-out;
  overflow: hidden;
}

// Animations
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(20px);
  }

  to {
    opacity: 1;
    transform: translateX(0);
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

// Responsive breakpoint
@media (max-width: 768px) {
  .layout-container {
    padding: 0.5rem;
  }
}
</style>
