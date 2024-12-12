import API_CODE from "~/utils/api_code";
import { get, post, del, put } from "./BaseService";

const createCertificate = async (params, success, error) => {
    await post(API_CODE.API_CREATE_CERTIFICATE, params, success, error);
};

const getCertificates = async (success, error, params) => {
    await get(API_CODE.API_GET_CERTIFICATE, success, error, params);
};

const getCertificateById = async (endpoint, success, error) => {
    await get(API_CODE.API_GET_CERTIFICATE_ID + endpoint, success, error);
};

const deleteCertificate = async (endpoint, data, success, error) => {
    await del(API_CODE.API_DELETE_CERTIFICATE + endpoint, data, success, error);
};

const changeStatusCertificate = async (endpoint, params, success, error) => {
    await put(API_CODE.API_CERTIFICATE_STATUS + endpoint, params, success, error);
}

export const CertificateAPI = {
    createCertificate,
    getCertificates,
    getCertificateById,
    deleteCertificate,
    changeStatusCertificate,
}