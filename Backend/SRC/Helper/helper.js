// StatusCode Helper

import { info } from "node:console";

const statusCodes = {
    OK: 200,
    CREATED: 201,
    MOVED_PERMANENTLY: 301,
    FOUND_TEMPORARILY: 302,
    BAD_REQUEST: 400,
    UNAUTHORIZED: 403,
    FORBIDDEN: 403,
    NOT_FOUND: 404,
    INTERNAL_SERVER_ERROR: 500,
    BAD_GATEWAY: 502,
    SERVICE_UNAVAILABLE: 503,
    GATEWAY_TIMEOUT: 504,
};

statusCodes.OK = {
    code: 200,
    message: 'OK',
    info: 'The request has succeeded.',
}

statusCodes.CREATED = {
    code: 201,
    message: 'Created',
    info: 'The request has been fulfilled and has been created.',
}

statusCodes.MOVED_PERMANENTLY = {
    code: 301,
    message: 'Moved Permanently',
    info: 'The requested resource has been permanently moved to a new URL. The client should use the new URL for future requests.',
}

export default statusCodes;