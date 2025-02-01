import { defineStore } from 'pinia'

export const useNetworkStore = defineStore('network-store', () => {
  const envUrl = 'http://localhost:3000'
  // env.VITE_API_URL = "http://localhost:3000";
  function getBaseUrl(url?: string, defaultUrl?: string | undefined | null) {
    return transformBaseUrl(envUrl ?? defaultUrl ?? url)
  }

  function transformBaseUrl(url: string) {
    if (!window) return
    url ??= ''
    //replace {host} with the actual host
    url = url.replace('{host}', window.location.host) //example.com:8080
    url = url.replace('{hostname}', window.location.hostname) //example.com
    url = url.replace('{protocol}', window.location.protocol) //https:
    url = url.replace('{port}', window.location.port) //8080
    url = url.replace('{origin}', window.location.origin) //https://example.com:8080
    return url
  }

  const apiUrl = ref(getBaseUrl())

  const fetchData = async (
    endpoint: string,
    options: {
      headers?: { [key: string]: string }
      method: 'GET' | 'POST' | 'PUT' | 'DELETE'
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      body: any
    },
  ) => {
    try {
      const response = await fetch(`${getBaseUrl()}${endpoint}`, {
        headers: {
          'Content-Type': 'application/json',
          ...options.headers, // Add any custom headers here
        },
        method: options.method || 'GET', // Default method is GET
        body: JSON.stringify(options.body) || null, // For POST, PUT, etc.
      })

      // Check if the response is okay
      if (!response.ok) {
        throw new Error(`Error: ${response.status}`)
      }

      // Parse response JSON data
      const data = await response.json()
      return data
    } catch (error) {
      console.error('API Error:', error)
      throw error // Re-throw the error to handle it in components
    }
  }

  watch(
    () => window?.location,
    () => {
      apiUrl.value = getBaseUrl()
    },
  )

  onMounted(() => {
    apiUrl.value = getBaseUrl()
  })

  const states = { apiUrl }

  const functions = { fetchData }

  return { ...states, ...functions }
})
