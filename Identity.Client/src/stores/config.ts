import { defineStore } from 'pinia'

export const useConfigStore = defineStore('config', () => {
  const config = ref<{
    apiUrl: string
  }>({
    apiUrl: '{origin}',
  })

  const states = {
    config: readonly(config),
  }

  function transformBaseUrl(url: string) {
    url ??= ''
    //replace {host} with the actual host
    url = url.replace('{host}', window.location.host) //example.com:8080
    url = url.replace('{hostname}', window.location.hostname) //example.com
    url = url.replace('{protocol}', window.location.protocol) //https:
    url = url.replace('{port}', window.location.port) //8080
    url = url.replace('{origin}', window.location.origin) //https://example.com:8080
    return url
  }

  const functions = { transformBaseUrl }

  return { ...states, ...functions }
})
