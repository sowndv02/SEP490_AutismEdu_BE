import API_CODE from "~/utils/api_code";
import { get, post, put } from "./BaseService";

const registerAsTutor = async (params, success, error) => {
    await post(API_CODE.API_TUTOR_REGISTER, params, success, error);
};
const listTutor = async (success, error, params) => {
    await get(API_CODE.API_TUTOR_LIST, success, error, params);
};

const handleRegistrationForm = async (endpoint, params, success, error) => {
    await put(API_CODE.API_TUTOR_STATUS + endpoint, params, success, error)
};

const handleGetTutors = async (success, error, params) => {
    await get(API_CODE.API_GET_TUTORS, success, error, params);
};
const handleGetTutor = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_TUTOR + endpoint, success, error);
};

const handleGetTutorProfile = async (success, error) => {
    await get(API_CODE.API_GET_TUTOR_PROFILE, success, error);
};

const handleUpdateTutorProfile = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_TUTOR_PROFILE + endpoint, params, success, error);
};

const handleGetTutorUpdateRequest = async (success, error, params) => {
    await get(API_CODE.API_GET_TUTOR_UPDATE_REQUEST, success, error, params);
};

const handleUpdateTutorChangeStatus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_PUT_TUTOR_CHANGE_STATUS + endpoint, params, success, error);
};

const handleGetTutorRegisterDetail = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_TUTOR_REGISTER_DETAIL + endpoint, success, error);
};
export const TutorManagementAPI = {
    registerAsTutor,
    listTutor,
    handleRegistrationForm,
    handleGetTutors,
    handleGetTutor,
    handleGetTutorProfile,
    handleUpdateTutorProfile,
    handleGetTutorUpdateRequest,
    handleUpdateTutorChangeStatus,
    handleGetTutorRegisterDetail
}