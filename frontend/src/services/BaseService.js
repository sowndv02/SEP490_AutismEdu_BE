import Cookies from 'js-cookie';

let api = null;
let prefix = '';

// Initializes the service with an axios instance and prefix
const initializeService = (axiosInstance, prefixValue) => {
  api = axiosInstance;
  prefix = prefixValue;
};

// Processes the response, returning the 'data' property if it exists
const processResponse = (response) => {
  const data = response.data;
  return data.hasOwnProperty('data') ? data.data : data;
};

// Logs and handles errors, specifically handling 401 unauthorized status
const logError = (e, error) => {
  if (error && e.response) {
    if (e.response.hasOwnProperty('status')) {
      if (e.response.status === 401) {
        Cookies.remove('access_token');
        Cookies.remove('role');
        const previousUserId = Cookies.get('user_id') || '';
        const queryString = `${window.location.pathname}${window.location.search}`;
        Cookies.set('previous_user_id', previousUserId, { expires: 90 });
        Cookies.set('redirectPath', queryString, { expires: 90 });
        window.location.href = '/login';
        return;
      }

      const errors = e.response.data.errors
        ? Object.fromEntries(
            Object.entries(e.response.data.errors).map(([key, value]) => [key, value[0]])
          )
        : [];
      error({
        code: e.response.status,
        error: errors,
        responseCode: e.response.data.responseCode,
      });
    }
  }
};

// Performs a GET request
const get = async (endpoint, success, error, params = {}) => {
  await api
    .get(prefix + endpoint, { params })
    .then(processResponse)
    .then(success)
    .catch((e) => logError(e, error));
};

// Performs a POST request
const post = async (endpoint, params = {}, success, error) => {
  await api
    .post(prefix + endpoint, params)
    .then(processResponse)
    .then(success)
    .catch((e) => logError(e, error));
};

// Performs a PUT request
const put = async (endpoint, params = {}, success, error) => {
  await api
    .put(prefix + endpoint, params)
    .then(processResponse)
    .then(success)
    .catch((e) => logError(e, error));
};

// Performs a DELETE request
const del = async (endpoint, data = {}, success, error) => {
  await api
    .delete(prefix + endpoint, { data })
    .then(processResponse)
    .then(success)
    .catch((e) => logError(e, error));
};

// Performs a PATCH request
const patch = async (endpoint, params = {}, success, error) => {
  await api
    .patch(prefix + endpoint, params)
    .then(processResponse)
    .then(success)
    .catch((e) => logError(e, error));
};

// Parses an object into a URL query string
const urlParse = (obj, query = false) => {
  const str = Object.entries(obj)
    .filter(([, value]) => value !== null && value !== undefined && value !== '')
    .map(([key, value]) => {
      if (value === true) value = 1;
      if (value === false) value = 0;
      return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`;
    });

  return query ? `?${str.join('&')}&${query}` : `?${str.join('&')}`;
};

export const method = {
  initializeService,
  get,
  post,
  put,
  del,
  patch,
  urlParse
};