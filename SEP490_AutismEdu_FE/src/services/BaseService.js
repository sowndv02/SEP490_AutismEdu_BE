import Cookies from 'js-cookie';
import { enqueueSnackbar } from 'notistack';

let api = null;
let prefix = '';

const initializeService = (axiosInstance, prefixValue) => {
  api = axiosInstance.axiosInstance;
  prefix = prefixValue;
};

const processResponse = (response) => {
  const data = response.data;
  return data?.data ? data.data : data;
};

const logError = (e, error) => {

  if (error && e.response) {

    if (e.response?.status) {
      if (e.response.status === 401) {
        Cookies.remove('access_token');
        Cookies.remove('refresh_token');
        if (window.location.href.includes("autismedu")) {
          window.location.href = '/autismedu/login';
        } else {
          window.location.href = '/autismtutor/tutor-login';
        }
        return;
      }
      if (e.response.status === 403) {
        Cookies.remove('access_token');
        Cookies.remove('refresh_token');
        if (window.location.href.includes("admin")) {
          window.location.href = '/admin/login';
        } else {
          window.location.href = '/autismtutor/tutor-login';
        }
        return;
      }
      if (e.response.status === 402) {
        window.location.href = '/autismtutor/payment-package-focus';
        enqueueSnackbar("Hết hạn gói đăng ký!", { variant: "error" })
        return;
      }

      error({
        code: e.response.data.statusCode,
        error: e.response.data.errorMessages,
        isSuccess: e.response.data.isSuccess
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

export {
  initializeService,
  get,
  post,
  put,
  del,
  patch,
  urlParse
};