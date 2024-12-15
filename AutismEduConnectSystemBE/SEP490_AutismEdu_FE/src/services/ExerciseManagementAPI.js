import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';


const getAllExerciseType = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_EXERCISE_TYPE, success, error, params);
};
const getExerciseByType = async (endpoint, success, error, params) => {
    await get(API_CODE.API_GET_EXERCISE_BY_TYPE + endpoint, success, error, params);
};
const getListExerciseType = async (success, error, params) => {
    await get(API_CODE.API_GET_LIST_EXERCISE_TYPE, success, error, params);
};

const getExerciseByTypeId = async (endpoint, success, error, params) => {
    await get(API_CODE.API_GET_EXERCISE_BY_TYPE_ID + endpoint, success, error, params);
};

const getAllExercise = async (success, error, params) => {
    await get(API_CODE.API_GET_EXERCISE, success, error, params);
};

const createExercise = async (params, success, error) => {
    await post(API_CODE.API_CREATE_EXERCISE, params, success, error);
};

const deleteExercise = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_EXERCISE + endpoint, data, success, error);
};

const changeStatus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_CHANGE_STATUS_ETYPE + endpoint, params, success, error)
};

const createExerciseType = async (params, success, error) => {
    await post(API_CODE.API_CREATE_EXERICSE_TYPE, params, success, error);
};

const updateExerciseType = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_EXERCISE_TYPE + endpoint, params, success, error);
};

const updateExercise = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_EXERCISE + endpoint, params, success, error);
};

export const ExerciseManagementAPI = {
    getAllExerciseType,
    getExerciseByType,
    getListExerciseType,
    getExerciseByTypeId,
    getAllExercise,
    createExercise,
    deleteExercise,
    changeStatus,
    createExerciseType,
    updateExerciseType,
    updateExercise,
}