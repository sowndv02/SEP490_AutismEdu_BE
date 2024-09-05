import { post, get } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getClaims = async (success, error, params) => {
    await get(API_CODE.API_GET_CLAIM, success, error, params);
};

export const ClaimManagementAPI = {
    getClaims
}