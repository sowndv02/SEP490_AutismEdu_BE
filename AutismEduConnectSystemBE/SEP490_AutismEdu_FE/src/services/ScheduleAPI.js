import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getSchedule = async (success, error, params) => {
    await get(API_CODE.API_GET_SCHEDULE, success, error, params);
};

const getScheduleById = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_SCHEDULE_BY_ID + endpoint, success, error);
};
const getAssignSchedule = async (success, error, params) => {
    await get(API_CODE.API_GET_ASSIGNED_SCHEDULE, success, error, params);
};

const updateAssignExercises = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_ASSIGN_EXERCISES + endpoint, params, success, error);
};

const updateScheduleChangeStatus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_SCHEDULE_CHANGE_STATUS + endpoint, params, success, error);
}

export const ScheduleAPI = {
    getSchedule,
    updateAssignExercises,
    getScheduleById,
    updateScheduleChangeStatus,
    getAssignSchedule
}