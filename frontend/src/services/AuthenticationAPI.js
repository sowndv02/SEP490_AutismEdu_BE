import { post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

// Function to handle login
const login = async (params, success, error) => {
  await post(API_CODE.API_001, params, success, error);
};

const getData = async (params, success, error) => {
  await get(API_CODE.API_003, success, error, params);
};

// Function to handle logout
const logout = async (params, success, error) => {
  await post(API_CODE.API_002, params, success, error);
};


const forgotPassword = async (params, success, error) => {
  await post(API_CODE.API_004, params, success, error);
}

export const AuthenticationAPI = {
  login,
  logout,
  getData,
  forgotPassword
};
