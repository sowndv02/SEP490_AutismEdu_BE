import { del, post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createAvailableTime = async (params, success, error) => {
    await post(API_CODE.API_CREATE_AVAILABLE_TIME, params, success, error);
};
const getAvailableTime = async (success, error, params) => {
    await get(API_CODE.API_GET_AVAILABLE_TIME, success, error, params);
};
const removeAvailableTime = async (endpoint, data, success, error) => {
    await del(API_CODE.API_REMOVE_AVAILABLE_TIME + endpoint, data, success, error);
};

export const AvailableTimeManagementAPI = {
    createAvailableTime,
    getAvailableTime,
    removeAvailableTime,
}