<script setup lang="ts">
import { UserDto } from '@/resources/api-clients/identity-api-client';


const userStore = useUserStore()

const user = ref<UserDto>()
watch(() => userStore.user, (newUser) => {
    user.value = new UserDto({ ...newUser });
}, { immediate: true });

</script>

<template>
    <div class="account-overview">
        <div class="background"></div>
        <div
            v-if="user"
            class="wrapper"
        >
            <Card class="account-card">
                <template #content>
                    <h1>Account Overview</h1>
                    <div class="fields">
                        <div class="input-label">
                            <label for="email">Email</label>
                            <InputText
                                id="email"
                                :value="user.email"
                                readonly
                                disabled
                            />
                        </div>
                        <div class="input-label">
                            <label for="username">Username</label>
                            <InputText
                                id="username"
                                v-model="user.userName"
                            />
                        </div>
                    </div>
                    <div class="buttons">
                        <Button
                            type="submit"
                            class="button-wide"
                            severity="success"
                            :loading="userStore.isLoading"
                            @click="userStore.updateUser(user)"
                        >
                            Update</Button>
                    </div>

                    <Divider />
                    <div class="buttons">
                        <Button
                            @click="userStore.logout()"
                            class="button-wide"
                            severity="warn"
                            outlined
                        >Logout</Button>
                    </div>
                </template>
            </Card>
            <!-- <p>Email: {{ user.email }}</p>
        <p>Joined: {{ new Date(user.joinedDate).toLocaleDateString() }}</p> -->
        </div>
        <div
            v-else
            class="wrapper"
        >
            <p>Loading...</p>
            <Skeleton
                width="20rem"
                height="2rem"
            />
        </div>
    </div>

</template>

<style lang="scss" scoped>
h1 {
    font-size: 2em;
    margin-bottom: 0.5em;
}

p {
    font-size: 1.2em;
    margin: 0.2em 0;
}


.buttons {
    display: flex;
    justify-content: flex-end;
    margin-top: 1em;

    .button-wide {
        width: 100%;
    }

    .button-right {
        margin-left: auto;
        justify-self: end;
    }
}

.input-label {
    display: flex;
    flex-direction: column;
    margin-bottom: 1em;
}

.account-overview {
    display: grid;


    .wrapper {
        display: grid;
        grid-template-rows: 15dvh auto 1fr;
        grid-column: 1;
        grid-row: 1;
        place-items: center;

        .account-card {
            grid-row: 2;
            grid-column: 1;
            gap: 1em;
            padding: 1rem;
            // border-radius: 0.5em;
            // border: 1px solid #ccc;
            width: 30rem;

        }
    }
}

.background {
    grid-row: 1;
    grid-column: 1;
}
</style>