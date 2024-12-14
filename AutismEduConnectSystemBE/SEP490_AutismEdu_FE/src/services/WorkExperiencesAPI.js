import { del, get, post, put } from "./BaseService";
import API_CODE from "~/utils/api_code";

const changeWorkExperienceStatus = async (endpoint, success, error, params) => {
    await put(API_CODE.API_WORK_EXPERIENCE_STATUS + endpoint, success, error, params);
};

const getWorkExperiences = async (success, error, params) => {
    await get(API_CODE.API_GET_WORK_EXPERIENCES, success, error, params);
};

const createWorkExperience = async (params, success, error) => {
    await post(API_CODE.API_CREATE_WORK_EXPERIENCE, params, success, error);
};

const deleteWorkExperience = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_WORK_EXPERIENCE + endpoint, data, success, error)
};

export const WorkExperiencesAPI = {
    changeWorkExperienceStatus,
    getWorkExperiences,
    createWorkExperience,
    deleteWorkExperience,
}