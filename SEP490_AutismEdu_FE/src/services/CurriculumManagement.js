import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createCurriculum = async (params, success, error) => {
    await post(API_CODE.API_CREATE_CURRICULUM, params, success, error);
};
const getCurriculums = async (success, error, params) => {
    await get(API_CODE.API_GET_CURRICULUMS, success, error, params);
};
const getUpdateRequest = async (success, error, params) => {
    await get(API_CODE.API_GET_UPDATE_REQUEST, success, error, params);
};
const changeStatusCurriculum = async (endpoint, params, success, error) => {
    await put(API_CODE.API_CHANGE_STATUS_CURRICULUM + endpoint, params, success, error)
};
const deleteCurriculum = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_CURRICULUM + endpoint, data, success, error);
};
export const CurriculumManagementAPI = {
    createCurriculum,
    getCurriculums,
    changeStatusCurriculum,
    getUpdateRequest,
    deleteCurriculum,
}