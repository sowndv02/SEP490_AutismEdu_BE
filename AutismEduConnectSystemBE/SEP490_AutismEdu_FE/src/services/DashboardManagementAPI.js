import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';


const getTotalParentHaveStudentProfile = async (success, error, params) => {
    await get(API_CODE.API_GET_TOTAL_PARENT_HAVE_STUDENT_PROFILE, success, error, params);
};
const getTotalUser = async (success, error, params) => {
    await get(API_CODE.API_GET_TOTAL_USER, success, error, params);
};
const getPackagePayment = async (success, error, params) => {
    await get(API_CODE.API_GET_PACKAGEPAYMENT, success, error, params);
};
const getRevenues = async (success, error, params) => {
    await get(API_CODE.API_GET_REVENUES, success, error, params);
};

export const DashboardManagementAPI = {
    getTotalParentHaveStudentProfile,
    getTotalUser,
    getPackagePayment,
    getRevenues
}