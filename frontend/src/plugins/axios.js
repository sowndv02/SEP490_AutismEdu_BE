import axios from 'axios';
import Cookies from 'js-cookie';

const headers = {
  'x-locale': Cookies.get('CurrentLanguage') || 'ja'
};

const token = Cookies.get('access_token');
if (token != undefined && token.length != 0) {
  headers.Authorization = 'Bearer ' + token;
}

const axiosInstance = axios.create({
  baseURL: "https://api.restful-api.dev/", // replace project base url later
  timeout: 80000,
  headers,
});

axiosInstance.interceptors.request.use(request => {
  if (token !== undefined) {
    request.headers.Authorization = `Bearer ${token}`;
  }
  return request;
});

const setHeaders = function(headers) {
  axiosInstance.defaults.headers.common = { ...axiosInstance.defaults.headers.common, ...headers };
}

export default {
  axiosInstance,
  setHeaders
};
