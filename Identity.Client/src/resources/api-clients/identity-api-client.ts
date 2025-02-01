//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

/* tslint:disable */
/* eslint-disable */
// ReSharper disable InconsistentNaming

import { useConfigStore } from '@/stores/config'
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

export class IdentityClient extends ClientBase {
    private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(baseUrl?: string, http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }) {
        super();
        this.http = http ? http : window as any;
        this.baseUrl = this.getBaseUrl("https://localhost:7105", baseUrl);
    }

    hi(): Promise<string> {
        let url_ = this.baseUrl + "/api/hi";
        url_ = url_.replace(/[?&]$/, "");

        let options_: RequestInit = {
            method: "GET",
            headers: {
                "Accept": "application/json"
            }
        };

        return this.transformOptions(options_).then(transformedOptions_ => {
            return this.http.fetch(url_, transformedOptions_);
        }).then((_response: Response) => {
            return this.transformResult(url_, _response, (_response: Response) => this.processHi(_response));
        });
    }

    protected processHi(response: Response): Promise<string> {
        const status = response.status;
        let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
        if (status === 200) {
            return response.text().then((_responseText) => {
            let result200: any = null;
            let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = resultData200 !== undefined ? resultData200 : <any>null;
    
            return result200;
            });
        } else if (status !== 200 && status !== 204) {
            return response.text().then((_responseText) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            });
        }
        return Promise.resolve<string>(null as any);
    }

    test(): Promise<void> {
        let url_ = this.baseUrl + "/api/test";
        url_ = url_.replace(/[?&]$/, "");

        let options_: RequestInit = {
            method: "GET",
            headers: {
            }
        };

        return this.transformOptions(options_).then(transformedOptions_ => {
            return this.http.fetch(url_, transformedOptions_);
        }).then((_response: Response) => {
            return this.transformResult(url_, _response, (_response: Response) => this.processTest(_response));
        });
    }

    protected processTest(response: Response): Promise<void> {
        const status = response.status;
        let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
        if (status === 200) {
            return response.text().then((_responseText) => {
            return;
            });
        } else if (status !== 200 && status !== 204) {
            return response.text().then((_responseText) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            });
        }
        return Promise.resolve<void>(null as any);
    }

    login(dto: LoginRequest): Promise<void> {
        let url_ = this.baseUrl + "/api/login";
        url_ = url_.replace(/[?&]$/, "");

        const content_ = JSON.stringify(dto);

        let options_: RequestInit = {
            body: content_,
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            }
        };

        return this.transformOptions(options_).then(transformedOptions_ => {
            return this.http.fetch(url_, transformedOptions_);
        }).then((_response: Response) => {
            return this.transformResult(url_, _response, (_response: Response) => this.processLogin(_response));
        });
    }

    protected processLogin(response: Response): Promise<void> {
        const status = response.status;
        let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
        if (status === 200) {
            return response.text().then((_responseText) => {
            return;
            });
        } else if (status !== 200 && status !== 204) {
            return response.text().then((_responseText) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            });
        }
        return Promise.resolve<void>(null as any);
    }

    logout(): Promise<void> {
        let url_ = this.baseUrl + "/api/logout";
        url_ = url_.replace(/[?&]$/, "");

        let options_: RequestInit = {
            method: "POST",
            headers: {
            }
        };

        return this.transformOptions(options_).then(transformedOptions_ => {
            return this.http.fetch(url_, transformedOptions_);
        }).then((_response: Response) => {
            return this.transformResult(url_, _response, (_response: Response) => this.processLogout(_response));
        });
    }

    protected processLogout(response: Response): Promise<void> {
        const status = response.status;
        let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
        if (status === 200) {
            return response.text().then((_responseText) => {
            return;
            });
        } else if (status !== 200 && status !== 204) {
            return response.text().then((_responseText) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            });
        }
        return Promise.resolve<void>(null as any);
    }
}

export class LoginRequest implements ILoginRequest {
    email?: string;
    password?: string;
    twoFactorCode?: string | undefined;
    twoFactorRecoveryCode?: string | undefined;

    constructor(data?: ILoginRequest) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.email = _data["email"];
            this.password = _data["password"];
            this.twoFactorCode = _data["twoFactorCode"];
            this.twoFactorRecoveryCode = _data["twoFactorRecoveryCode"];
        }
    }

    static fromJS(data: any): LoginRequest {
        data = typeof data === 'object' ? data : {};
        let result = new LoginRequest();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["email"] = this.email;
        data["password"] = this.password;
        data["twoFactorCode"] = this.twoFactorCode;
        data["twoFactorRecoveryCode"] = this.twoFactorRecoveryCode;
        return data;
    }
}

export interface ILoginRequest {
    email?: string;
    password?: string;
    twoFactorCode?: string | undefined;
    twoFactorRecoveryCode?: string | undefined;
}

export class ApiException extends Error {
    override message: string;
    status: number;
    response: string;
    headers: { [key: string]: any; };
    result: any;

    constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
        super();

        this.message = message;
        this.status = status;
        this.response = response;
        this.headers = headers;
        this.result = result;
    }

    protected isApiException = true;

    static isApiException(obj: any): obj is ApiException {
        return obj.isApiException === true;
    }
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): any {
    if (result !== null && result !== undefined)
        throw result;
    else
        throw new ApiException(message, status, response, headers, null);
}

/* eslint-disable @typescript-eslint/no-explicit-any */

/*
// i mport { useAlertStore } from '@/stores/alert';
 !
 ! Recreate api client after modifying this file to see changes
 !
 */