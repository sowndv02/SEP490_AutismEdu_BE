import API_CODE from "~/utils/api_code";
import { post } from "./BaseService";

const registerAsTutor = async (params, success, error) => {
    await post(API_CODE.API_TUTOR_REGISTER, params, success, error);
};

export const TutorManagementAPI = {
    registerAsTutor
}