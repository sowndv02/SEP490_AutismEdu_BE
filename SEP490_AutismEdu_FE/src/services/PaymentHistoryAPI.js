import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';


const createPaymentHistory = async (params, success, error) => {
    await post(API_CODE.API_POST_PAYMENT_HISTORY, params, success, error);
};

const getListPaymentHistory = async (success, error, params) => {
    await get(API_CODE.API_GET_PAYMENT_HISTORY, success, error, params);
};
const getListPaymentHistoryCurrent = async (success, error) => {
    await get(API_CODE.API_GET_PAYMENT_HISTORY_CURRENT, success, error);
};


export const PaymentHistoryAPI = {
    createPaymentHistory,
    getListPaymentHistory,
    getListPaymentHistoryCurrent
}