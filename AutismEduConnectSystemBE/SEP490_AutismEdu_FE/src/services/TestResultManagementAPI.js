import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createSubmitTest = async (params, success, error) => {
    await post(API_CODE.API_CREATE_TEST_RESULT, params, success, error);
};

const getListTestResultHistory = async (success, error, params) => {
    await get(API_CODE.API_GET_TEST_RESULT_HISTORY, success, error, params);
};

const getTestResultDetailHistory = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_TEST_RESULT_DETAIL_HISTORY + endpoint, success, error);
}

export const TestResultManagementAPI = {
    createSubmitTest,
    getListTestResultHistory,
    getTestResultDetailHistory,
}