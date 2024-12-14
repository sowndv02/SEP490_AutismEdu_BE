import { get, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const getAllPaymentPackage = async (success, error, params) => {
    await get(API_CODE.API_GET_NOTIFICATION, success, error, params)
}
const readAPaymentPackage = async (endpoint, params, success, error) => {
    await put(API_CODE.API_READ_A_NOTIFICATION + endpoint, params, success, error)
}
const readAllPaymentPackage = async (params, success, error) => {
    await put(API_CODE.API_READ_ALL_NOTIFICATION, params, success, error)
}

export const NotificationAPI = {
    getAllPaymentPackage,
    readAPaymentPackage,
    readAllPaymentPackage
}