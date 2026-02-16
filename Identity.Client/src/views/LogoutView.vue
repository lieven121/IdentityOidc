<script setup lang="ts">
import { IdentityClient } from '@/resources/api-clients/identity-api-client';
import { RouteNames } from '@/router/routes';
const identityClient = new IdentityClient()
const router = useRouter()

onMounted(async () => {
  await identityClient.logout()
  router.push({ name: RouteNames.Login })
})

</script>

<style lang="scss" scoped></style>
