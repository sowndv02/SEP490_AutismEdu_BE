import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createTutorReport = async (params, success, error) => {
    await post(API_CODE.API_CREATE_REPORT_TUTOR, params, success, error);
};
const createReviewReport = async (params, success, error) => {
    await post(API_CODE.API_CREATE_REPORT_REVIEW, params, success, error);
};

const getListReportTutor = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_REPORT, success, error, params)
}

const getReportDetail = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_REPORT_DETAIL + endpoint, success, error)
}

const changeReportStatus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_CHANGE_REPORT_STATUS + endpoint, params, success, error);
};
export const ReportManagementAPI = {
    createTutorReport,
    getListReportTutor,
    getReportDetail,
    changeReportStatus,
    createReviewReport
}