namespace backend_api
{
    public static class SD
    {
        // Const default account
        public static string ADMIN_EMAIL_DEFAULT = "admin@admin.com";
        public static string ADMIN_PASSWORD_DEFAULT = "Sa12345@";

        // Const role
        public const string USER_ROLE = "User";
        public const string PARENT_ROLE = "Parent";
        public const string STAFF_ROLE = "Staff";
        public const string ADMIN_ROLE = "Admin";
        public const string TUTOR_ROLE = "Tutor";

        // Const image user
        public const string URL_IMAGE_USER = "UserImages";
        public const string URL_IMAGE_DEFAULT = "https://placehold.co/600x400";
        public const string IMAGE_DEFAULT_AVATAR_NAME = "default-avatar.png";
        public const string URL_FE = "http://localhost:5173";
        public const string URL_FE_FULL = "http://localhost:5173/autismedu";
        public const string URL_IMAGE_DEFAULT_BLOB = "https://storagesep.blob.core.windows.net/logos-public/default-avatar.png";

        // Const folder mail save
        public const string FOLDER_NAME_LOG_EMAIL = "MailSave";

        // Const user type
        public const string APPLICATION_USER = "ApplicationUser";
        public const string GOOGLE_USER = "GoogleUser";

        // Const refresh token
        public const string APPLICATION_REFRESH_TOKEN = "Application";
        public const string GOOGLE_REFRESH_TOKEN = "Google";


        // Const claim 
        public const string DEFAULT_CREATE_CLAIM_TYPE = "Create";
        public const string DEFAULT_CREATE_CLAIM_VALUE = "True";
        public const string DEFAULT_DELETE_CLAIM_TYPE = "Delete";
        public const string DEFAULT_DELETE_CLAIM_VALUE = "True";
        public const string DEFAULT_UPDATE_CLAIM_TYPE = "Update";
        public const string DEFAULT_UPDATE_CLAIM_VALUE = "True";
        public const string DEFAULT_VIEW_CLAIM_TYPE = "View";
        public const string DEFAULT_VIEW_CLAIM_VALUE = "True";

        public const string DEFAULT_CREATE_CLAIM_CLAIM_TYPE = "Create";
        public const string DEFAULT_CREATE_CLAIM_CLAIM_VALUE = "Claim";

        public const string DEFAULT_UPDATE_CLAIM_CLAIM_TYPE = "Update";
        public const string DEFAULT_UPDATE_CLAIM_CLAIM_VALUE = "Claim";

        public const string DEFAULT_DELETE_CLAIM_CLAIM_TYPE = "Delete";
        public const string DEFAULT_DELETE_CLAIM_CLAIM_VALUE = "Claim";

        public const string DEFAULT_CREATE_ROLE_CLAIM_TYPE = "Create";
        public const string DEFAULT_CREATE_ROLE_CLAIM_VALUE = "Role";

        public const string DEFAULT_UPDATE_ROLE_CLAIM_TYPE = "Update";
        public const string DEFAULT_UPDATE_ROLE_CLAIM_VALUE = "Role";

        public const string DEFAULT_DELETE_ROLE_CLAIM_TYPE = "Delete";
        public const string DEFAULT_DELETE_ROLE_CLAIM_VALUE = "Role";

        public const string DEFAULT_CREATE_USER_CLAIM_TYPE = "Create";
        public const string DEFAULT_CREATE_USER_CLAIM_VALUE = "User";

        public const string DEFAULT_UPDATE_USER_CLAIM_TYPE = "Update";
        public const string DEFAULT_UPDATE_USER_CLAIM_VALUE = "User";

        public const string DEFAULT_DELETE_USER_CLAIM_TYPE = "Delete";
        public const string DEFAULT_DELETE_USER_CLAIM_VALUE = "User";

        public const string DEFAULT_ASSIGN_ROLE_CLAIM_TYPE = "Assign";
        public const string DEFAULT_ASSIGN_ROLE_CLAIM_VALUE = "Role";

        public const string DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE = "Assign";
        public const string DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE = "Claim";

        public const string DEFAULT_VIEW_CLAIM_CLAIM_TYPE = "View";
        public const string DEFAULT_VIEW_CLAIM_CLAIM_VALUE = "Claim";

        public const string DEFAULT_VIEW_TUTOR_CLAIM_TYPE = "View";
        public const string DEFAULT_VIEW_TUTOR_CLAIM_VALUE = "Tutor";

        public const string DEFAULT_VIEW_CERTIFICATE_CLAIM_TYPE = "View";
        public const string DEFAULT_VIEW_CERTIFICATE_CLAIM_VALUE = "Certificate";

        public const string DEFAULT_UPDATE_CERTIFICATE_CLAIM_TYPE = "Update";
        public const string DEFAULT_UPDATE_CERTIFICATE_CLAIM_VALUE = "Certificate";

        // Const license name
        public const string BACHELORS_DEGREE_LICENSE = "Bằng đại học";
        public const string CITIZEN_ID_LICENSE = "Căn cước công dân";
        public const string OTHER_LICENSE = "Khác";

        // Const message 
        public const string TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR = "You are already a tutor or have a previous registration requirement";
        public const string TUTOR_UPDATE_STATUS_IS_PENDING = "Không thể cập nhật trạng thái thành đang xử lý!";

        public enum Status 
        {
            PENDING = 2,
            APPROVE = 1,
            REJECT = 0
        }

    }
}
