<script setup lang="ts">
import {
  startAuthentication,
  browserSupportsWebAuthn,
  startRegistration,
} from '@simplewebauthn/browser'
import Password from 'primevue/password'
import type { ExternalLoginProvider } from './ExternalLoginProvider';
import { ApiException, IdentityClient, LoginRequest } from '@/resources/api-clients/identity-api-client';


const props = withDefaults(defineProps<{
  position?: 'left' | 'center' | 'right';

  logo?: string;
  showPasswordDefault?: boolean;

  supportPasskeySignIn?: boolean;
  externalProviders?: ExternalLoginProvider[]
}>(), {
  position: 'left',

  showPasswordDefault: false,

  supportPasskeySignIn: false,
  externalProviders: () => [],
});

const router = useRouter()
const networkStore = useNetworkStore()

const identityClient = new IdentityClient();

const internalLoading = ref(false)
const loading = computed(() => {
  if (loggingIn.value) return true

  return internalLoading.value
})

const username = ref('')
const usernameDisabled = computed(() => {
  if (loading.value) return true
})
const usernameError = ref(false)

const showPassword = ref(props.showPasswordDefault)
const password = ref('')
const passwordError = ref(false)

const showOtp = ref(false)
const otp = ref('')
const otpError = ref(false)
watch(otp, (val) => {
  if (val.length == 6) {
    login()
  }
})

const loggingIn = ref(false)

function triggerPassword() {
  if (username.value.length > 0) {
    if (!showPassword.value) {
      showPassword.value = true
      setTimeout(() => {
        document.getElementById('PasswordInput')?.focus()
      }, 100)
    }
  }
}

async function login() {
  try {
    loggingIn.value = true

    if (!showPassword.value) {
      showPassword.value = true
      setTimeout(() => {
        document.getElementById('PasswordInput')?.focus()
      }, 100)
      if (password.value == '') return
    }

    //try to login

    if (username.value.length < 3) {
      usernameError.value = true
      passwordError.value = true
      return
    }

    try {

      await identityClient.login(new LoginRequest({
        email: username.value,
        password: password.value,
        twoFactorCode: otp.value,
      }))
      loginSuccesful()

    } catch (error) {
      if ((error instanceof ApiException)) {
        console.error(error)
        if (error.status == 401) {
          usernameError.value = true
          passwordError.value = true
          otpError.value = true
          return
        }
        if (error.status == 202) {
          showOtpInput()
        }
      }

    }

    // if (true && !showOtp.value) {
    //   showOtp.value = true
    //   setTimeout(() => {
    //     const otpInput = document.getElementById('OtpInput')
    //     const firstInput = otpInput?.querySelector('input')
    //     if (firstInput) {
    //       firstInput.focus()
    //     }
    //   }, 100)
    //   if (otp.value == '') return
    // }


    console.log('Logging in with name:', username.value)
  } catch (error) {
    console.error(error)
  } finally {
    loggingIn.value = false
  }
}

function showOtpInput() {
  showOtp.value = true
  setTimeout(() => {
    const otpInput = document.getElementById('OtpInput')
    const firstInput = otpInput?.querySelector('input')
    if (firstInput) {
      firstInput.focus()
    }
  }, 100)
}

async function loginWithPasskey(register = false) {
  try {
    loggingIn.value = true
    if (!browserSupportsWebAuthn()) {
      console.log('WebAuthn not supported')
      return
    }

    const data = await networkStore.fetchData('/api/fido/credential-options', {
      method: 'POST',
      body: {
        username: username.value,
        password: '',
      },
    })
    // const options = useFetch('/fido/credential-options', {
    //     method: 'POST',
    //     body: JSON.stringify({
    //         username: username.value
    //     })
    // })
    if (register) {
      // data.username = username.value
      const registerResult = await startRegistration(data)
      console.log('Registering with passkey', registerResult)
    } else {
      const result = await startAuthentication(data)
      console.log('Logging in with passkey', result, username.value)
    }
  } catch (error) {
    console.error(error)
  } finally {
    loggingIn.value = false
  }
}

function loginSuccesful() {
  //redirect to ReturnUrl otherwise to home using vue router
  console.log('Login successful')

  const returnUrl = new URLSearchParams(window.location.search).get('ReturnUrl');
  if (returnUrl) {
    window.location.href = returnUrl;
  } else {
    router.push({ name: 'Account' });
  }

}

const webAuthnSupported = ref(false)

onMounted(() => {
  webAuthnSupported.value = browserSupportsWebAuthn()
})
</script>

<template>
  <div
    class="login-form"
    :class="position"
  >

    <div class="login-background"></div>
    <div class="login-wrapper">
      <Card class="login-card">
        <template #content>
          <div class="logo-wrapper"><img
              class="logo"
              :src="logo"
              alt="Logo"
            /></div>
          <h2>Login</h2>
          <form class="inputs">
            <div class="input-label">
              <InputText
                id="UsernameInput"
                autofocus="true"
                autocomplete="email"
                :invalid="usernameError"
                v-model="username"
                :disabled="usernameDisabled"
                @keydown.enter="login"
                @focusout="triggerPassword"
              ></InputText>
              <label for="UsernameInput">Username</label>
            </div>

            <div
              class="input-label"
              v-show="showPassword"
            >
              <Password
                :inputProps="{ autocomplete: 'current-password' }"
                :invalid="passwordError"
                autocomplete="password"
                v-model="password"
                input-id="PasswordInput"
                :feedback="false"
                @keydown.enter="login"
                toggle-mask
              ></Password>
              <label for="PasswordInput">Password</label>
            </div>

            <div
              class="input-label otp"
              v-if="showOtp"
            >
              <InputOtp
                :length="6"
                :invalid="otpError"
                aria-autocomplete="otp"
                v-model="otp"
                id="OtpInput"
              ></InputOtp>
              <label for="OtpInput">OTP</label>
            </div>
          </form>

          <div class="buttons">
            <Button
              severity="primary"
              @click="login"
              :disabled="loading"
            >Login</Button>
            <Button
              v-if="webAuthnSupported && supportPasskeySignIn"
              severity="secondary"
              @click="loginWithPasskey(false)"
              :disabled="loading"
            >
              <i class="fa-duotone fa-solid fa-key"></i>
              Login with Passkey</Button>
          </div>

          <div
            class="third-party"
            v-if="externalProviders.length"
          >
            <Divider />
            <div class="options">
              <Button
                severity="secondary"
                @click="loginWithPasskey(true)"
              >
                <i class="fa-brands fa-google"></i>
              </Button>
              <Button
                severity="secondary"
                @click="loginWithPasskey(false)"
              ><i class="fa-brands fa-microsoft"></i>
              </Button>
            </div>
          </div>
        </template>
      </Card>
    </div>
  </div>

</template>

<style lang="scss" scoped>
.login-form {
  display: grid;

  height: 100%;
  min-height: 0;
  width: 100%;
  min-width: 0;
  grid-template-columns: var(--desktop-layout-columns, auto);
  --wrapper-border: var(--border-width) solid var(--p-neutral-500);
  --layout-panel-width: var(--login-wrapper-width, 40rem);


  &.left {
    --desktop-layout-columns: var(--layout-panel-width) 1fr;

    .login-wrapper {
      border-right: var(--wrapper-border);
      grid-column: 1;
    }

    .login-background {
      grid-column: 2;
    }
  }

  &.center {
    --desktop-layout-columns: auto;
  }

  &.right {
    --desktop-layout-columns: 1fr var(--layout-panel-width);

    .login-wrapper {
      border-left: var(--wrapper-border);
      grid-column: 2;
    }

    .login-background {
      grid-column: 1;
    }
  }

  .login-wrapper {
    padding: 1rem;
    background-color: var(--login-wrapper-color);
    display: grid;
    grid-template-rows: var(--login-wrapper-top-spacing, 15dvh) auto 1fr;
    place-items: center;
    grid-row: 1;
    grid-column: 1;
  }

  .login-background {
    background-color: transparent;
    grid-row: 1;
    grid-column: 1;
  }

  @media (max-width: 1200px) {
    grid-template-columns: var(--mobile-layout-columns, auto);
    --login-wrapper-top-spacing: 10dvh;

    .login-wrapper {
      grid-column: 1;
    }

    .login-background {
      // display: none;
    }
  }
}

.login-card {
  width: 100%;
  grid-row: 2;
  max-width: 30rem;
  // margin: 0 auto;
  // margin-top: 10rem;

  .p-card-body {
    display: flex;
    flex-direction: column;
    align-items: center;

    .logo-wrapper {
      display: flex;

      .logo {
        max-width: 80%;
        margin: 0 auto;
      }
    }


    h2 {
      text-align: center;
    }

    .inputs,
    .buttons {
      width: 100%;
      display: flex;
      flex-direction: column;

      &>*:not(:last-child) {
        margin-bottom: 0.5rem;
      }
    }

    .third-party {
      .options {
        gap: 0.5rem;
        display: flex;
        justify-content: center;
      }
    }

    .input-label {
      display: grid;
      grid-template-rows: auto 1fr;

      label {
        grid-row: 1;
      }
    }

    .input-label.otp {
      .p-inputotp {
        justify-content: center;
      }
    }

    .inputs {
      margin-bottom: 1rem;

      .p-floatlabel {
        margin-top: 2rem;
      }

      .p-password {
        width: 100%;
      }
    }
  }
}
</style>
