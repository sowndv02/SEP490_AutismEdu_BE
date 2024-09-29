import { post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getClaims = async (success, error, params) => {
    await get(API_CODE.API_GET_CLAIM, success, error, params);
};
const addClaim = async (params, success, error) => {
    await post(API_CODE.API_ADD_CLAIM, params, success, error);
};

export const ClaimManagementAPI = {
    getClaims,
    addClaim,
}