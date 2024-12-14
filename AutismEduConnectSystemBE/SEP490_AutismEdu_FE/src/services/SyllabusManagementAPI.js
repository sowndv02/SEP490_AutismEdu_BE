import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createSyllabus = async (params, success, error) => {
    await post(API_CODE.API_CREATE_SYLLABUS, params, success, error);
};

const getListSyllabus = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_SYLLABUS, success, error, params);
};

const assignExerciseSyllabus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_ASSIGN_SYLLABUS + endpoint, params, success, error);
};

const deleteSyllabus = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_SYLLABUS + endpoint, data, success, error);
}


export const SyllabusManagementAPI = {
    createSyllabus,
    getListSyllabus,
    assignExerciseSyllabus,
    deleteSyllabus,
}