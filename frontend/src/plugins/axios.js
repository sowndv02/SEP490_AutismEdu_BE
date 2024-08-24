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
  baseURL: "https://backend-api20240823212838.azurewebsites.net/", // replace project base url later
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