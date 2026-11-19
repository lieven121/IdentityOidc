<script setup lang="ts">
import { RouteNames } from '@/router/routes'

const route = useRoute()
const router = useRouter()

// Friendly wording for the OpenID Connect errors a user can end up on; anything else falls
// back to a generic message. The raw code and description stay visible for whoever debugs it.
const messages: Record<string, { title: string; text: string }> = {
  invalid_request: {
    title: 'Sign-in request not accepted',
    text: "The application that sent you here made a sign-in request we can't accept. This is usually a configuration problem in that application.",
  },
  invalid_client: {
    title: 'Unknown application',
    text: "The application that sent you here isn't registered with this sign-in service.",
  },
  unauthorized_client: {
    title: 'Application not allowed',
    text: "The application that sent you here isn't allowed to sign in this way.",
  },
  invalid_scope: {
    title: 'Permissions not available',
    text: "The application asked for permissions that it can't be given.",
  },
  access_denied: {
    title: 'Access denied',
    text: "You don't have access to this application.",
  },
  not_found: {
    title: 'Page not found',
    text: "The page you're looking for doesn't exist.",
  },
}

const fallback = {
  title: 'Something went wrong',
  text: "We couldn't complete your request. Please try again later.",
}

const firstQueryValue = (value: unknown) =>
  (Array.isArray(value) ? value[0] : value)?.toString() || undefined

const code = computed(
  () =>
    firstQueryValue(route.query.error) ??
    (route.name === RouteNames.NotFound ? 'not_found' : undefined),
)
const description = computed(() => firstQueryValue(route.query.error_description))
const message = computed(() => (code.value && messages[code.value]) || fallback)

const canGoBack = computed(() => window.history.length > 1)
const goBack = () => window.history.back()
const goHome = () => router.push({ name: RouteNames.Root })
</script>

<template>
  <div class="error-view">
    <div class="error-background"></div>
    <div class="error-wrapper">
      <Card class="error-card">
        <template #content>
          <div class="logo-wrapper"><img class="logo" src="/images/logo.svg" alt="Logo" /></div>
          <i class="error-icon fa-duotone fa-triangle-exclamation" aria-hidden="true"></i>
          <h2>{{ message.title }}</h2>
          <p class="error-text">{{ message.text }}</p>

          <div v-if="code && code !== 'not_found'" class="error-details">
            <div class="detail-row">
              <span class="detail-label">Error</span>
              <code>{{ code }}</code>
            </div>
            <div v-if="description" class="detail-row">
              <span class="detail-label">Details</span>
              <span>{{ description }}</span>
            </div>
          </div>

          <div class="buttons">
            <Button
              v-if="canGoBack"
              label="Go back"
              severity="secondary"
              outlined
              @click="goBack"
            />
            <Button label="Go to my account" @click="goHome" />
          </div>
        </template>
      </Card>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.error-view {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  height: 100%;
  min-height: 0;

  .error-background,
  .error-wrapper {
    grid-row: 1;
    grid-column: 1;
  }

  .error-background {
    background-image: url('/images/bg.jpg');
    background-repeat: no-repeat;
    background-size: cover;
    background-position: right;
    background-position-x: 80%;
  }

  .error-wrapper {
    padding: 1rem;
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    grid-template-rows: 15dvh auto 1fr;
    place-items: center;

    @media (max-width: 1200px) {
      grid-template-rows: 10dvh auto 1fr;
    }
  }
}

.error-card {
  width: 100%;
  grid-row: 2;
  max-width: 30rem;

  :deep(.p-card-body) {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
  }

  .logo-wrapper {
    display: flex;

    .logo {
      max-width: 80%;
      margin: 1rem auto;
    }
  }

  .error-icon {
    font-size: 2.5rem;
    color: var(--p-orange-500);
  }

  h2 {
    margin: 0.75rem 0 0.25rem;
  }

  .error-text {
    margin: 0 0 1rem;
    color: var(--p-text-muted-color);
  }

  .error-details {
    width: 100%;
    margin-bottom: 1.5rem;
    padding: 0.75rem 1rem;
    border-radius: var(--p-border-radius-md, 6px);
    background: var(--p-content-hover-background, rgba(127, 127, 127, 0.1));
    text-align: left;
    font-size: 0.875rem;
    overflow-wrap: anywhere;

    .detail-row {
      display: grid;
      grid-template-columns: 4.5rem 1fr;
      gap: 0.5rem;

      &:not(:last-child) {
        margin-bottom: 0.25rem;
      }
    }

    .detail-label {
      color: var(--p-text-muted-color);
    }
  }

  .buttons {
    width: 100%;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;

    & > * {
      flex: 1;
    }
  }
}
</style>
