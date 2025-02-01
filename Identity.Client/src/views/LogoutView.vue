<script setup lang="ts">
import { IdentityClient } from '@/resources/api-clients/identity-api-client';

const identityClient = new IdentityClient()
const router = useRouter()

onMounted(async () => {
    await identityClient.logout()
    router.push({ name: 'Login' })
})

</script>

<style lang="scss" scoped></style>