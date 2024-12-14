import { post, get, put, del } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getStudentTimeSlot = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_STUDENT_TIME_SLOT + endpoint, success, error);
}

const createTimeSlot = async (endpoint, params, success, error) => {
    await post(API_CODE.API_CREATE_SCHEDULE + endpoint, params, success, error)
}
const deleteTimeSlot = async (endpoint, success, error) => {
    await del(API_CODE.API_DELETE_SCHEDULE + endpoint, {}, success, error)
}
const updateTimeSlot = async (params, success, error) => {
    await put(API_CODE.API_UPDATE_SCHEDULE, params, success, error)
}
const updateSlot = async (params, success, error) => {
    await put(API_CODE.API_UPDATE_TIME_SLOT, params, success, error)
}

export const TimeSlotAPI = {
    getStudentTimeSlot,
    createTimeSlot,
    deleteTimeSlot,
    updateTimeSlot,
    updateSlot
}