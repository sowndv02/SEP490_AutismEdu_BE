import { post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

// Function to handle login
const login = async (params, success, error) => {
  await post(API_CODE.API_001, params, success, error);
};

const resetPassword = async (params, success, error) => {
  await post(API_CODE.API_003, params, success, error);
};

// Function to handle logout
const logout = async (params, success, error) => {
  await post(API_CODE.API_002, params, success, error);
};


const forgotPassword = async (params, success, error) => {
  await post(API_CODE.API_004, params, success, error);
}

const verifyAccount = async (params, success, error) => {
  await post(API_CODE.API_005, params, success, error);
}

const confirmEmail = async (params, success, error) => {
  await post(API_CODE.API_006, params, success, error);
}
const register = async (params, success, error) => {
  await post(API_CODE.API_007, params, success, error);
}

export const AuthenticationAPI = {
  login,
  logout,
  resetPassword,
  forgotPassword,
  verifyAccount,
  confirmEmail,
  register
};
