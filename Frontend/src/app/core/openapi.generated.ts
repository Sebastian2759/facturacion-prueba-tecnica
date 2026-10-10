/** Generated from the verified local backend OpenAPI export. Do not edit.
 * Canonical schema SHA-256: 90479d27aca3ba826e4e99f2c88e561e3f8354c1b3b823e5df0aa00605b96bda
 */
export interface paths {
  '/api/v1/accounts': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Code?: string;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfGetAccountsResponse'];
            'text/json': components['schemas']['ResponseBaseOfGetAccountsResponse'];
            'text/plain': components['schemas']['ResponseBaseOfGetAccountsResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody: {
        content: {
          'application/*+json': components['schemas']['CreateAccountRequest'];
          'application/json': components['schemas']['CreateAccountRequest'];
          'text/json': components['schemas']['CreateAccountRequest'];
        };
      };
      responses: {
        /** @description Created */
        201: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfCreateAccountResponse'];
            'text/json': components['schemas']['ResponseBaseOfCreateAccountResponse'];
            'text/plain': components['schemas']['ResponseBaseOfCreateAccountResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/invoices': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Page?: number | string;
          PageSize?: number | string;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfListInvoicesResponse'];
            'text/json': components['schemas']['ResponseBaseOfListInvoicesResponse'];
            'text/plain': components['schemas']['ResponseBaseOfListInvoicesResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: {
          'Idempotency-Key'?: string;
        };
        path?: never;
        cookie?: never;
      };
      requestBody: {
        content: {
          'application/*+json': components['schemas']['CreateInvoiceRequest'];
          'application/json': components['schemas']['CreateInvoiceRequest'];
          'text/json': components['schemas']['CreateInvoiceRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
            'text/json': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
            'text/plain': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
          };
        };
        /** @description Created */
        201: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
            'text/json': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
            'text/plain': components['schemas']['ResponseBaseOfCreateInvoiceResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/invoices/{number}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          number: number;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
            'text/json': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
            'text/plain': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
          };
        };
        /** @description Not Found */
        404: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
            'text/json': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
            'text/plain': components['schemas']['ResponseBaseOfGetInvoiceByNumberResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/invoices/preview': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody: {
        content: {
          'application/*+json': components['schemas']['PreviewInvoiceRequest'];
          'application/json': components['schemas']['PreviewInvoiceRequest'];
          'text/json': components['schemas']['PreviewInvoiceRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfPreviewInvoiceResponse'];
            'text/json': components['schemas']['ResponseBaseOfPreviewInvoiceResponse'];
            'text/plain': components['schemas']['ResponseBaseOfPreviewInvoiceResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/session': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'application/json': components['schemas']['ResponseBaseOfSessionDto'];
            'text/json': components['schemas']['ResponseBaseOfSessionDto'];
            'text/plain': components['schemas']['ResponseBaseOfSessionDto'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/version': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content?: never;
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/health/live': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content?: never;
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
}
export type webhooks = Record<string, never>;
export interface components {
  schemas: {
    AccountDto: {
      additionalAddress: null | string;
      address: null | string;
      cityCode: string;
      code: string;
      countryCode: string;
      /** Format: int32 */
      customerType: number | string;
      departmentCode: string;
      document: string;
      email: string;
      externalCode: null | string;
      firstName: null | string;
      identificationType: string;
      lastName: null | string;
      name: string;
      /** Format: int32 */
      personType: number | string;
      phone: null | string;
      /** Format: date-time */
      registeredAt: string;
      website: null | string;
    };
    CreateAccountRequest: {
      additionalAddress?: null | string;
      address?: null | string;
      cityCode?: string;
      code?: string;
      countryCode?: string;
      /** Format: int32 */
      customerType?: number | string;
      departmentCode?: string;
      document?: string;
      email?: string;
      externalCode?: null | string;
      firstName?: null | string;
      identificationType?: string;
      lastName?: null | string;
      name?: string;
      /** Format: int32 */
      personType?: number | string;
      phone?: null | string;
      /** Format: date-time */
      registeredAt?: string;
      website?: null | string;
    };
    CreateAccountResponse: {
      accepted: boolean;
      code: string;
    };
    CreateInvoiceRequest: {
      address?: string;
      details?: components['schemas']['InvoiceLineInput'][];
      document?: string;
      firstName?: string;
      lastName?: string;
      phone?: string;
    };
    CreateInvoiceResponse: {
      invoice: components['schemas']['InvoiceDetailDto'];
    };
    GetAccountsResponse: {
      accounts: components['schemas']['AccountDto'][];
    };
    GetInvoiceByNumberResponse: {
      invoice: components['schemas']['InvoiceDetailDto'];
    };
    HttpStatusCode: number;
    InvoiceDetailDto: {
      address?: string;
      /** Format: date-time */
      date?: string;
      details?: components['schemas']['InvoiceLineDto'][];
      /** Format: double */
      discount?: number | string;
      document?: string;
      firstName?: string;
      /** Format: double */
      gross?: number | string;
      /** Format: uuid */
      id?: string;
      lastName?: string;
      /** Format: int32 */
      number?: number | string;
      phone?: string;
      /** Format: double */
      tax?: number | string;
      /** Format: double */
      total?: number | string;
    };
    InvoiceLineDto: {
      /** Format: int32 */
      code?: number | string;
      /** Format: double */
      lineTotal?: number | string;
      /** Format: int32 */
      quantity?: number | string;
      /** Format: double */
      unitPrice?: number | string;
    };
    InvoiceLineInput: {
      /** Format: int32 */
      code?: number | string;
      /** Format: int32 */
      quantity?: number | string;
      /** Format: double */
      unitPrice?: number | string;
    };
    InvoiceListDto: {
      /** Format: date-time */
      date?: string;
      document?: string;
      firstName?: string;
      lastName?: string;
      /** Format: int32 */
      number?: number | string;
      /** Format: double */
      total?: number | string;
    };
    ListInvoicesResponse: {
      items: components['schemas']['InvoiceListDto'][];
      /** Format: int32 */
      page: number | string;
      /** Format: int32 */
      pageSize: number | string;
      /** Format: int32 */
      totalCount: number | string;
    };
    OidcSettings: {
      authority: string;
      clientId: string;
      scope: string;
    };
    PreviewInvoiceRequest: {
      details?: components['schemas']['InvoiceLineInput'][];
    };
    PreviewInvoiceResponse: {
      /** Format: double */
      discount: number | string;
      /** Format: double */
      gross: number | string;
      /** Format: double */
      tax: number | string;
      /** Format: double */
      total: number | string;
    };
    ResponseBaseOfCreateAccountResponse: {
      code?: string;
      data?: null | components['schemas']['CreateAccountResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfCreateInvoiceResponse: {
      code?: string;
      data?: null | components['schemas']['CreateInvoiceResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfGetAccountsResponse: {
      code?: string;
      data?: null | components['schemas']['GetAccountsResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfGetInvoiceByNumberResponse: {
      code?: string;
      data?: null | components['schemas']['GetInvoiceByNumberResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfListInvoicesResponse: {
      code?: string;
      data?: null | components['schemas']['ListInvoicesResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfPreviewInvoiceResponse: {
      code?: string;
      data?: null | components['schemas']['PreviewInvoiceResponse'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    ResponseBaseOfSessionDto: {
      code?: string;
      data?: null | components['schemas']['SessionDto'];
      errors?: null | string[];
      fieldErrors?: null | {
        [key: string]: string[];
      };
      isSuccess?: boolean;
      message?: string;
      statusCode?: components['schemas']['HttpStatusCode'];
      traceId?: null | string;
    };
    SessionDto: {
      authenticated: boolean;
      canEvaluate: boolean;
      canUseRealAccounts: boolean;
      csrfToken?: null | string;
      mode: string;
      oidc?: null | components['schemas']['OidcSettings'];
    };
  };
  responses: never;
  parameters: never;
  requestBodies: never;
  headers: never;
  pathItems: never;
}
export type $defs = Record<string, never>;
export type operations = Record<string, never>;
