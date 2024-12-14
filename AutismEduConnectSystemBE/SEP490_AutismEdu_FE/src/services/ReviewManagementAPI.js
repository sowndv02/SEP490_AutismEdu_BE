import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createReview = async (params, success, error) => {
    await post(API_CODE.API_CREATE_REVIEW, params, success, error);
};

const getReviewStats = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_REVIEW_STATS + endpoint, success, error);
};

const getReviewForTutor = async (success, error, params) => {
    await get(API_CODE.API_GET_REVIEW, success, error, params);
};

const updateReview = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_REVIEW + endpoint, params, success, error);
};

const deleteReview = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_REVIEW + endpoint, data, success, error);
};


export const ReviewManagementAPI = {
    createReview,
    getReviewStats,
    updateReview,
    deleteReview,
    getReviewForTutor,
}