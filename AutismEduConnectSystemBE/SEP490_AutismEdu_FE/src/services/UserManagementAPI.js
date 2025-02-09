import { del, get, post, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getUsers = async (success, error, params) => {
    await get(API_CODE.API_GET_USERS, success, error, params);
};
const lockUsers = async (endpoint, success, error) => {
    await get(API_CODE.API_LOCK_USERS + endpoint, success, error);
};
const unLockUsers = async (endpoint, success, error) => {
    await get(API_CODE.API_UNLOCK_USERS + endpoint, success, error);
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
const getUserRoles = async (endpoint, success, error, params) => {
    await get(API_CODE.API_GET_USER_ROLES + endpoint, success, error, params);
};
const getUserById = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_USER_ID + endpoint, success, error);
};
const removeUserRoles = async (endpoint, data, success, error) => {
    await del(API_CODE.API_REMOVE_USER_ROLES + endpoint, data, success, error);
};
const createUser = async (params, success, error) => {
    await post(API_CODE.API_CREATE_USER, params, success, error)
}

const updateUser = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_USER + endpoint, params, success, error)
}
const changePassword = async (endpoint, params, success, error) => {
    await put(API_CODE.API_CHANGE_PASSWORD + endpoint, params, success, error)
}

const getUserByEmail = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_USER_EMAIL + endpoint, success, error);
}
export const UserManagementAPI = {
    getUsers,
    lockUsers,
    unLockUsers,
    getUserClaims,
    assignClaims,
    removeUserClaims,
    getUserRoles,
    removeUserRoles,
    getUserById,
    createUser,
    updateUser,
    getUserByEmail,
    changePassword
}