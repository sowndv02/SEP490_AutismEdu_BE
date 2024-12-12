import API_CODE from "~/utils/api_code";
import { get, post, put } from "./BaseService";

const listAssessment = async (success, error) => {
    await get(API_CODE.API_GET_ASSESSMENT, success, error);
};
const createAssessment = async (params, success, error) => {
    await post(API_CODE.API_CREATE_ASSESSMENT, params, success, error);
};
const updateAssessment = async (params, success, error) => {
    await put(API_CODE.API_UPDATE_ASSESSMENT, params, success, error);
};
export const AssessmentManagementAPI = {
    listAssessment,
    createAssessment,
    updateAssessment
}