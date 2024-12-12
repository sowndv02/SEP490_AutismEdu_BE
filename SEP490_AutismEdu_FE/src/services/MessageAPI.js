import { get, post, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const sendMessages = async (params, success, error) => {
    await post(API_CODE.API_SEND_MESSAGE, params, success, error)
}
const getMessages = async (endpoint, success, error, params) => {
    await get(API_CODE.API_GET_MESSAGE + endpoint, success, error, params)
}
const readMessages = async (endpoint, params, success, error) => {
    console.log(endpoint);
    await put(API_CODE.API_READ_MESSAGE + endpoint, params, success, error)
}

export const MessageAPI = {
    sendMessages,
    getMessages,
    readMessages
}