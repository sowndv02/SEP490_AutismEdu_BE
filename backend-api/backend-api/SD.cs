namespace backend_api
{
    public static class SD
    {
        // Const default account
        public static string ADMIN_EMAIL_DEFAULT = "admin@admin.com";
        public static string ADMIN_PASSWORD_DEFAULT = "Sa12345@";

        // Const role
        public const string USER_ROLE = "User";
        public const string MANAGER_ROLE = "Manager";
        public const string PARENT_ROLE = "Parent";
        public const string STAFF_ROLE = "Staff";
        public const string ADMIN_ROLE = "Admin";
        public const string TUTOR_ROLE = "Tutor";

        // Const image user
        public const string URL_IMAGE_USER = "UserImages";
        public const string URL_IMAGE_DEFAULT = "https://placehold.co/600x400";
        public const string IMAGE_DEFAULT_AVATAR_NAME = "default-avatar.png";
        public const string URL_FE = "http://localhost:5173";
        public const string URL_FE_TUTOR_REGISTRATION_REQUEST = "http://localhost:5173/autismedu/tutor-registration";
        public const string URL_FE_FULL = "http://localhost:5173/autismedu";
        public const string URL_FE_TUTOR_LOGIN = "http://localhost:5173/autismtutor/tutor-login";
        public const string URL_IMAGE_DEFAULT_BLOB = "https://sep490g50v1.blob.core.windows.net/logos-public/default-avatar.png";

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
        public const string BAD_REQUEST_MESSAGE = "Dữ liệu không hợp lệ";
        public const string WEEKDAY_REQUIRED = "Chưa nhập ngày trong tuần";
        public const string TIMESLOT_REQUIRED = "Chưa nhập khung giờ";
        public const string TIMESLOT_INVALID = "Khung giờ không hợp lệ";
        public const string TIMESLOT_DUPLICATED = "Khung giờ mới bị trùng với khung giờ đã tồn tại";
        public const string NOT_FOUND_MESSAGE = "Không tìm thấy dữ liệu";
        public const string DUPLICATED_MESSAGE = "Dữ liệu đã tồn tại";
        public const string INTERNAL_SERVER_ERROR_MESSAGE = "Hệ thống đang xảy ra lỗi! Vui lòng thử lại sau!";
        public const string CHILD_NAME_DUPLICATE = "Đã tồn tại trẻ với tên này";
        public const string CHILD_ALREADY_STUDING_THIS_TUTOR = "Đã tồn tại hồ sơ học sinh của trẻ này";
		public const string NO_REVIEWS_FOUND = "Không có đánh giá";
        public const string REVIEW_ALREADY_EXISTS = "Đã đánh giá gia sư này";
        public const string BAD_ACTION_REVIEW = "Không tìm thấy đánh giá hoặc bạn không có quyền thay đổi đánh giá này";
        // enum status
        public enum Status
        {
            PENDING = 2,
            APPROVE = 1,
            REJECT = 0
        }

        // enum exercise pass status
        public enum PassingStatus
        {
            PASSED = 1,
            NOT_PASSED = 0
        }

        // enum schedule attend status
        public enum AttendanceStatus
        {
            NOT_YET = 2,
            ATTENDED = 1,
            ABSENT = 0
        }

        // enum reject type
        public enum RejectType
        {
            Approved = -1,
            IncompatibilityWithCurriculum = 1,
            SchedulingConflicts = 2,
            Other = 3
        }

        // enum student teaching status
        public enum StudentProfileStatus
        {
            Pening = 3,
            Reject = 2,
            Teaching = 1,
            Stop = 0
        }

        public const string IncompatibilityWithCurriculumMsg = "Không tương thích với chương trình giảng dạy";
        public const string SchedulingConflictsMsg = "Xung đột lịch trình";
        public const string OtherMsg = "Khác";


        // Status string
        public const string STATUS_PENDING = "pending";
        public const string STATUS_APPROVE = "approve";
        public const string STATUS_REJECT = "reject";
        public const string STATUS_ALL = "all";


        // sort Order
        public const string ORDER_DESC = "desc";
        public const string ORDER_ASC = "asc";

        // Order by
        public const string CREADTED_DATE = "createdDate";
        public const string AGE_FROM = "ageFrom";
    }
}
