import { get, post, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createConversations = async (params, success, error) => {
    await post(API_CODE.API_CREATE_CONVERSATION, params, success, error)
}
const getConversations = async (success, error, params) => {
    await get(API_CODE.API_GET_CONVERSATION, success, error, params)
}

export const ConversationAPI = {
    createConversations,
    getConversations
}