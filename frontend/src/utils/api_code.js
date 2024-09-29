const API_CODE = {
  // Auth
  API_001: '/v1/Auth/login',
  API_002: '/v1/Auth/register',
  API_003: '/v1/Auth/reset-password',
  API_004: '/v1/Auth/forgot-password',
  API_005: '/v1/Auth/resend-confirm-email',
  API_006: '/v1/Auth/confirm-email',
  API_007: '/v1/Auth/register',
  API_008: '/v1/Auth/get-token-external',
  API_009: '/v1/test/authorized-access',

  //User management
  API_GET_USERS: '/v1/User',
  API_LOCK_USERS: '/v1/User/lock/',
  API_UNLOCK_USERS: '/v1/User/unlock/',
  API_GET_USER_CLAIMS: '/v1/User/claim/',
  API_ASSIGN_CLAIMS: '/v1/User/claim/',
  API_REMOVE_USER_CLAIM: '/v1/User/claim/',
  API_ASSIGN_ROLES: '/v1/User/role/',
  API_GET_USER_ROLES: '/v1/User/role/',
  API_REMOVE_USER_ROLES: '/v1/User/role/',
  API_CREATE_USER: '/v1/User',
  API_GET_USER_ID: '/v1/User/',
  //Claim management
  API_GET_CLAIM: '/v1/Claim',
  API_ADD_CLAIM: '/v1/Claim',

  //role management
  API_GET_ROLE: '/v1/Role',
  API_ADD_ROLE: '/v1/Role'
};

export default API_CODE;
