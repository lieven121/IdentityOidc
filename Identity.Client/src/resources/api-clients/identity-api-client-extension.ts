/* eslint-disable @typescript-eslint/no-explicit-any */
import { useConfigStore } from '@/stores/config'

/*
// i mport { useAlertStore } from '@/stores/alert';
 !
 ! Recreate api client after modifying this file to see changes
 !
 */
export class ClientBase {
  //   private alertStore: any;
  private configStore

  public constructor() {
    this.configStore = useConfigStore()
    // this.alertStore = useAlertStore();
  }

  public getBaseUrl(url: string, defaultUrl: string | undefined | null) {
    return this.configStore.transformBaseUrl(this.configStore.config?.apiUrl ?? defaultUrl ?? url)
  }

  protected async transformOptions(options: RequestInit): Promise<RequestInit> {
    // await this.authStore.getAccessToken()
    options.headers = {
      ...options.headers,
      'Content-Type': 'application/json',
    }

    return options
  }

  protected async transformResult(
    url: string,
    response: Response,
    processor: (response: Response) => any,
  ) {
    // clone() throws a TypeError if the response body has already been used.
    // In fact, the main reason clone() exists is to allow multiple uses of body objects(when they are one - use only.)
    // https://developer.mozilla.org/en-US/docs/Web/API/Response/clone
    // if (response.status === 403 || response.status === 401) {
    //   if (window.location.pathname !== '/unauthorized') window.location.href = '/unauthorized'
    // } else
    if (400 <= response.status && response.status < 600 && response.body) {
      let errorText = undefined
      try {
        errorText = ((await response.clone().json()) as any)?.title
      } catch (e: Error | any) {
        console.error(e)
        // If not json then json() will throw an error
      }
      errorText ??= response.clone ? await response.clone().text() : response //   this.alertStore.error(await errorText, 10000);
    }
    return processor(response.clone())
  }
}
