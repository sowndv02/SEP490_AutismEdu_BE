import { post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createProgressReport = async (params, success, error) => {
    await post(API_CODE.API_CREATE_PROGRESS_REPORT, params, success, error);
};
const getListProgressReport = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_PROGRESS_REPORT, success, error, params);
};
const updateProgressReport = async (params, success, error) => {
    await put(API_CODE.API_UPDATE_PROGRESS_REPORT, params, success, error);
};

export const ProgressReportAPI = {
    createProgressReport,
    getListProgressReport,
    updateProgressReport
}