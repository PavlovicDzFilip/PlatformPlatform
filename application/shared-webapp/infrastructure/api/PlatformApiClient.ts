import type { ClientOptions, Middleware } from "openapi-fetch";
import createClient from "openapi-fetch";
import type { MediaType, OperationRequestBody, PathsWithMethod } from "openapi-typescript-helpers";
import {
  type ClientMethodWithProblemDetails,
  createClientMethodWithProblemDetails
} from "./ClientMethodWithProblemDetails";
import { isKeyof } from "@repo/utils/object/isKeyof";
import { createPlatformServerAction, type PlatformServerAction } from "./PlatformServerAction";
import { createApiReactHook, type PlatformApiReactHook } from "./ApiReactHook";

/**
 * Create a client for the platform API.
 * We use the openapi-fetch library to create a client for the platform API.
 * In addition to the standard client methods, we also provide a method to call server actions.
 *
 * Due to __"operational consistency"__ in `PlatformPlatform` all Api calls have a uniform behavior.
 * * `baseUrl` is set to the `import.meta.env.PUBLIC_URL` per default *(but can be overridden)*
 * * Data is returned by the api client otherwise errors are thrown as either `Error` or `ProblemDetailsError`
 * * Use the http methods `get`, `put`, `post`, `delete`, `options`, `head`, `patch`, `trace` to call the api
 * * Use the `action` method to call server actions *(Useful for interacting with Form data in React)*
 *
 * In PlatformPlatform all Api calls use the ProblemDetails standard and throw errors.
 */
export function createPlatformApiClient<
  // biome-ignore  lint/suspicious/noExplicitAny: We don't know the type at this point
  Paths extends Record<string, Record<string, any>>,
  Media extends MediaType = MediaType
>(clientOptions?: ClientOptions) {
  const client = createClient<Paths>({ baseUrl: import.meta.env.PUBLIC_URL, ...clientOptions });
  const notImplemented = (name: string | symbol) => {
    throw new Error(`Action client method not implemented: ${name.toString()}`);
  };

  return new Proxy({} as PlatformApiClient<Paths, Media>, {
    get(_, name) {
      switch (name) {
        case "useApi":
          return createApiReactHook(client.GET);
        case "actionPost":
          return createPlatformServerAction(client.POST);
        case "actionPut":
          return createPlatformServerAction(client.PUT);
        case "actionDelete":
          return createPlatformServerAction(client.DELETE);
        case "get":
          return <Path extends PathsWithMethod<Paths, "get">>(
            url: Path,
            options?: OperationRequestBody<Paths[Path]["get"]>
          ) => {
            return createClientMethodWithProblemDetails(client.GET)(
              url,
              options as OperationRequestBody<Paths[Path]["get"]>
            );
          };
        case "post":
          return <Path extends PathsWithMethod<Paths, "post">>(
            url: Path,
            options?: OperationRequestBody<Paths[Path]["post"]>
          ) => {
            return createClientMethodWithProblemDetails(client.POST)(
              url,
              options as OperationRequestBody<Paths[Path]["post"]>
            );
          };
        case "uploadFile":
          return <Path extends PathsWithMethod<Paths, "post">>(url: Path, file: File) => {
            const formData = new FormData();
            formData.append("file", file);

            return createClientMethodWithProblemDetails(client.POST)(url, { body: formData } as OperationRequestBody<
              Paths[Path]["post"]
            >);
          };
        case "put":
          return <Path extends PathsWithMethod<Paths, "put">>(
            url: Path,
            options?: OperationRequestBody<Paths[Path]["put"]>
          ) => {
            return createClientMethodWithProblemDetails(client.PUT)(
              url,
              options as OperationRequestBody<Paths[Path]["put"]>
            );
          };
        case "delete":
          return <Path extends PathsWithMethod<Paths, "delete">>(
            url: Path,
            options?: OperationRequestBody<Paths[Path]["delete"]>
          ) => {
            return createClientMethodWithProblemDetails(client.DELETE)(
              url,
              options as OperationRequestBody<Paths[Path]["delete"]>
            );
          };
        case "addMiddleware":
          return client.use;
        case "removeMiddleware":
          return client.eject;
        default:
          if (isKeyof(name, client)) {
            return client[name];
          }
          return notImplemented(name);
      }
    }
  });
}

type PlatformApiClient<Paths extends {}, Media extends MediaType = MediaType> = {
  /** Call a GET endpoint using a React hook with state management */
  useApi: PlatformApiReactHook<Paths, "get", Media>;
  /** Call a server POST action */
  actionPost: PlatformServerAction<Paths, "post", Media>;
  /** Call a server PUT action */
  actionPut: PlatformServerAction<Paths, "put", Media>;
  /** Call a server DELETE action */
  actionDelete: PlatformServerAction<Paths, "delete", Media>;
  /** Call a GET endpoint */
  get: ClientMethodWithProblemDetails<Paths, "get", Media>;
  /** Call a PUT endpoint */
  put: ClientMethodWithProblemDetails<Paths, "put", Media>;
  /** Call a POST endpoint */
  post: ClientMethodWithProblemDetails<Paths, "post", Media>;
  /** Call a POST endpoint  that accepts multipart/form-data */
  uploadFile: <Path extends PathsWithMethod<Paths, "post">>(
    url: Path,
    file: File
  ) => ReturnType<ClientMethodWithProblemDetails<Paths, "post", Media>>;
  /** Call a DELETE endpoint */
  delete: ClientMethodWithProblemDetails<Paths, "delete", Media>;
  /** Call a OPTIONS endpoint */
  options: ClientMethodWithProblemDetails<Paths, "options", Media>;
  /** Call a HEAD endpoint */
  head: ClientMethodWithProblemDetails<Paths, "head", Media>;
  /** Call a PATCH endpoint */
  patch: ClientMethodWithProblemDetails<Paths, "patch", Media>;
  /** Call a TRACE endpoint */
  trace: ClientMethodWithProblemDetails<Paths, "trace", Media>;
  /** Register middleware */
  addMiddleware(...middleware: Middleware[]): void;
  /** Unregister middleware */
  removeMiddleware(...middleware: Middleware[]): void;
};
