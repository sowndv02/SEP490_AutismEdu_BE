import { del, post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getRoles = async (success, error, params) => {
    await get(API_CODE.API_GET_ROLE, success, error, params);
};
const assignRoles = async (endpoint, params, success, error) => {
    console.log(endpoint);
    console.log(params);
    await post(API_CODE.API_ASSIGN_ROLES + endpoint, params, success, error);
};
const addRole = async (params, success, error) => {
    await post(API_CODE.API_ADD_ROLE, params, success, error);
};

export const RoleManagementAPI = {
    getRoles,
    assignRoles,
    addRole
}