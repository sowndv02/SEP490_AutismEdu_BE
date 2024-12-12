import { del, post, get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';


const createPaymentPackage = async (params, success, error) => {
    await post(API_CODE.API_POST_PAYMENT_PACKAGE, params, success, error);
};

const getListPaymentPackage = async (success, error, params) => {
    await get(API_CODE.API_GET_PAYMENT_PACKAGE, success, error, params)
};
const updatePaymentPackage = async (endpoint, params, success, error) => {
    await put(API_CODE.API_PUT_PAYMENT_PACKAGE + endpoint, params, success, error)
};


export const PackagePaymentAPI = {
    createPaymentPackage,
    getListPaymentPackage,
    updatePaymentPackage
}