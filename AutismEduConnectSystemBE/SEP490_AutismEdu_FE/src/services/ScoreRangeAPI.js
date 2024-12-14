import API_CODE from "~/utils/api_code";
import { del, get, post, put } from "./BaseService";

const createScoreRange = async (params, success, error) => {
    await post(API_CODE.API_CREATE_SCORE_RANGE, params, success, error);
};

const getListScoreRange = async (success, error) => {
    await get(API_CODE.API_GET_SCORE_RANGE, success, error)
}
const updateScoreRange = async (params, success, error) => {
    await put(API_CODE.API_EDIT_SCORE_RANGE, params, success, error)
}
const deleteScoreRange = async (endpoint, success, error) => {
    await del(API_CODE.API_DELETE_SCORE_RANGE + endpoint, {}, success, error)
}

export const ScoreRangeAPI = {
    createScoreRange,
    getListScoreRange,
    updateScoreRange,
    deleteScoreRange
}