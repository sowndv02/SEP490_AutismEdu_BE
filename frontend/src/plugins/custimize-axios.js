import axios from 'axios';
import Cookies from 'js-cookie';

// Khởi tạo headers ban đầu
let headers = {
  'x-locale': Cookies.get('CurrentLanguage') || 'ja',
};

let accessToken = Cookies.get('access_token');
const refreshToken = Cookies.get('refresh_token');

if (accessToken) {
  headers.Authorization = `Bearer ${accessToken}`;
}

const axiosInstance = axios.create({
  baseURL: 'https://sep490-backend.azurewebsites.net/', // Thay thế bằng base URL của dự án
  timeout: 40000,
  headers,
});

// Biến để kiểm soát việc làm mới token
let isRefreshing = false;
let refreshSubscribers = [];

// Hàm thông báo cho các yêu cầu đang chờ khi token mới đã được cập nhật
const onRrefreshed = (newToken) => {
  refreshSubscribers.map((callback) => callback(newToken));
  refreshSubscribers = [];
};

// Hàm thêm các yêu cầu vào hàng đợi trong khi đang làm mới token
const addRefreshSubscriber = (callback) => {
  refreshSubscribers.push(callback);
};

// Interceptor cho các yêu cầu (request)
axiosInstance.interceptors.request.use(
  (request) => {
    const token = Cookies.get('access_token');
    if (token) {
      request.headers.Authorization = `Bearer ${token}`;
    }
    return request;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor cho các phản hồi (response)
axiosInstance.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    const originalRequest = error.config;

    // Kiểm tra nếu lỗi là 401 và không phải là yêu cầu làm mới token
    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Nếu đang làm mới token, thêm yêu cầu vào hàng đợi
        return new Promise((resolve) => {
          addRefreshSubscriber((newToken) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            resolve(axiosInstance(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      // Gọi API để làm mới token
      return new Promise((resolve, reject) => {
        axios
          .post('/auth/refresh-token', { refreshToken }) // Thay thế URL và payload phù hợp với API của bạn
          .then((res) => {
            if (res.status === 200) {
              const newAccessToken = res.data.accessToken;
              Cookies.set('access_token', newAccessToken);

              axiosInstance.defaults.headers.common.Authorization = `Bearer ${newAccessToken}`;
              originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

              onRrefreshed(newAccessToken);
              resolve(axiosInstance(originalRequest));
            } else {
              // Nếu làm mới token thất bại, thực hiện logout
              Cookies.remove('access_token');
              Cookies.remove('refresh_token');
              window.location.href = '/login';
              reject(error);
            }
          })
          .catch((err) => {
            Cookies.remove('access_token');
            Cookies.remove('refresh_token');
            window.location.href = '/login';
            reject(err);
          })
          .finally(() => {
            isRefreshing = false;
          });
      });
    }

    return Promise.reject(error);
  }
);

const setHeaders = (customHeaders) => {
  axiosInstance.defaults.headers.common = {
    ...axiosInstance.defaults.headers.common,
    ...customHeaders,
  };
};

export default {
  axiosInstance,
  setHeaders,
};
