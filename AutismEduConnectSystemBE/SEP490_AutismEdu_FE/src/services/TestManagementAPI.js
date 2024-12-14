import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createTest = async (params, success, error) => {
    await post(API_CODE.API_CREATE_TEST, params, success, error);
};

const getListTest = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_TEST, success, error, params);
};

export const TestManagementAPI = {
    createTest,
    getListTest,
}