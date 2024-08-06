import { method } from './BaseService';
import API_CODE from '~/utils/api_code';

// Function to handle login
const login = async (params, success, error) => {
  await method.post(API_CODE.API_001, params, success, error);
};

// Function to handle logout
const logout = async (params, success, error) => {
  await method.post(API_CODE.API_002, params, success, error);
};

export const AuthenticationAPI = {
  login,
  logout,
};
