import API_CODE from "~/utils/api_code";
import { get, post, put } from "./BaseService";

const listChildren = async (endpoint, success, error) => {
    await get(API_CODE.API_CHILDREN_LIST + endpoint, success, error);
};

const createChild = async (params, success, error) => {
    await post(API_CODE.API_CREATE_CHILD, params, success, error)
}

const updateChild = async (params, success, error) => {
    await put(API_CODE.API_UPDATE_CHILD, params, success, error)
}
export const ChildrenManagementAPI = {
    listChildren,
    createChild,
    updateChild
}