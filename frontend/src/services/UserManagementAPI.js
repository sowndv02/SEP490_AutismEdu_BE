import { del, post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getUsers = async (success, error, params) => {
    await get(API_CODE.API_GET_USERS, success, error, params);
};
const lockUsers = async (endpoint, success, error, params) => {
    await get(API_CODE.API_LOCK_USERS + endpoint, success, error, params);
};
const unLockUsers = async (endpoint, success, error, params) => {
    await get(API_CODE.API_UNLOCK_USERS + endpoint, success, error, params);
};
const getUserClaims = async (endpoint, success, error, params) => {
    await get(API_CODE.API_GET_USER_CLAIMS + endpoint, success, error, params);
};
const assignClaims = async (endpoint, params, success, error) => {
    await post(API_CODE.API_ASSIGN_CLAIMS + endpoint, params, success, error);
}
const removeUserClaims = async (endpoint, data, success, error) => {
    await del(API_CODE.API_REMOVE_USER_CLAIM + endpoint, data, success, error);
}
export const UserManagementAPI = {
    getUsers,
    lockUsers,
    unLockUsers,
    getUserClaims,
    assignClaims,
    removeUserClaims
}