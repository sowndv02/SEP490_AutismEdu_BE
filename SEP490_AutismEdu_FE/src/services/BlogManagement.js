import { get, post, put } from '~/services/BaseService';
import API_CODE from '~/utils/api_code';

const createBlog = async (params, success, error,) => {
    await post(API_CODE.API_CREATE_BLOG, params, success, error)
}
const getBlogs = async (success, error, params) => {
    await get(API_CODE.API_GET_BLOGS, success, error, params)
}
const getBlogDetail = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_BLOG_DETAIL + endpoint, success, error)
}
const updateBlogStatus = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_BLOG_STATUS + endpoint, params, success, error)
}
const updateBlog = async (endpoint, params, success, error) => {
    await put(API_CODE.API_UPDATE_BLOG + endpoint, params, success, error)
}

export const BlogAPI = {
    createBlog,
    getBlogs,
    getBlogDetail,
    updateBlogStatus,
    updateBlog
}