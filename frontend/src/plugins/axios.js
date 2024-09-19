import axios from 'axios';
import Cookies from 'js-cookie';

const headers = {
  "Content-Type": "application/json"
};

const token = Cookies.get('access_token');
if (token != undefined && token.length != 0) {
  headers.Authorization = 'Bearer ' + token;
}

const axiosInstance = axios.create({
<<<<<<< HEAD
  baseURL: "https://localhost:5000/", // replace project base url later
=======
  baseURL: "https://sep490-g50.azurewebsites.net/",
  timeout: 80000, // replace project base url later
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
  headers
});


let isRefreshing = false;

axiosInstance.interceptors.request.use(request => {
  let newAccesstToken = Cookies.get('access_token');
  if (newAccesstToken !== undefined) {
    request.headers.Authorization = `Bearer ${newAccesstToken}`;
  }
  return request;
});


axiosInstance.interceptors.response.use(function (response) {
  return response;
}, async function (error) {
  const originalRequest = error.config;
  if (error.response.status === 401 && !originalRequest._retry) {
<<<<<<< HEAD
      originalRequest._retry = true;
      if (!isRefreshing) {
          isRefreshing = true;
          try {
              const newToken = await refreshAccessToken();
              axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
              axiosInstance.defaults.headers.Authorization = `Bearer ${newToken}`;
              originalRequest.headers.Authorization = `Bearer ${newToken}`;
              isRefreshing = false;
              return axiosInstance(originalRequest);
          } catch (err) {
              isRefreshing = false;
              return Promise.reject(err);
          }
      } else {
          return new Promise((resolve, reject) => {
              const checkRefresh = setInterval(() => {
                  const token = Cookies.get("access_token")
                  if (!isRefreshing) {
                      clearInterval(checkRefresh);
                      if (token) {
                          originalRequest.headers.Authorization = `Bearer ${token}`;
                          resolve(axiosInstance(originalRequest));
                      } else {
                          reject(error);
                      }
                  }
              }, 1000);
          });
      }
=======
    originalRequest._retry = true;
    if (!isRefreshing) {
      isRefreshing = true;
      try {
        const newToken = await refreshAccessToken();
        axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
        axiosInstance.defaults.headers.Authorization = `Bearer ${newToken}`;
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        isRefreshing = false;
        return axiosInstance(originalRequest);
      } catch (err) {
        isRefreshing = false;
        return Promise.reject(err);
      }
    } else {
      return new Promise((resolve, reject) => {
        const checkRefresh = setInterval(() => {
          const token = Cookies.get("access_token")
          if (!isRefreshing) {
            clearInterval(checkRefresh);
            if (token) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              resolve(axiosInstance(originalRequest));
            } else {
              reject(error);
            }
          }
        }, 1000);
      });
    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
  }
  return Promise.reject(error);
});

async function refreshAccessToken() {
  try {
<<<<<<< HEAD
      const response = await axiosInstance.post('/api/v1/Auth/refresh', { refreshToken: Cookies.get("refresh_token"), accessToken: Cookies.get("access_token")});
      Cookies.set("access_token", response.data.result.accessToken)
      Cookies.set("refresh_token", response.data.result.refreshToken)
      return response.data.result.accessToken;
  } catch (error) {
      console.error('Failed to refresh access token:', error);
      throw error;
  }
}
const setHeaders = function(headers) {
=======
    const response = await axiosInstance.post('/api/v1/Auth/refresh', { refreshToken: Cookies.get("refresh_token"), accessToken: Cookies.get("access_token") });
    Cookies.set("access_token", response.data.result.accessToken)
    Cookies.set("refresh_token", response.data.result.refreshToken)
    return response.data.result.accessToken;
  } catch (error) {
    console.error('Failed to refresh access token:', error);
    throw error;
  }
}
const setHeaders = function (headers) {
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
  axiosInstance.defaults.headers.common = { ...axiosInstance.defaults.headers.common, ...headers };
}

export default {
  axiosInstance,
  setHeaders
};