namespace backend_api
{
    public static class SD
    {
        // Const default account
        public static string ADMIN_EMAIL_DEFAULT = "admin@admin.com";
        public static string ADMIN_PASSWORD_DEFAULT = "AQAAAAEAACcQAAAAEOjDDzJdPlquEhfypVX/5nwm9uhcqeJuN4AO5jXlmTQ+/Ql9BqVmT6x5hCi1eS1BsQ==";
        public static string ADMIN_ADDRESS_DEFAULT = "Khu Giáo dục và Đào tạo – Khu Công nghệ cao Hòa Lạc – Km29 Đại lộ Thăng Long, H. Thạch Thất, TP. Hà Nội";
        public static string ADMIN_PHONENUMBER_DEFAULT = "0999999999";

        // Const role
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
        public const string URL_FE_STUDENT_PROFILE_DETAIL = "http://localhost:5173/autismedu/approve-student-profile/";
        public const string URL_FE_PARENT_LOGIN = "";
        public const string URL_FE_TUTOR_SETTING = "/autismtutor/tutor-setting";
        public const string URL_FE_TUTOR = "/autismtutor";
        public const string URL_FE_TUTOR_STUDENT_PROFILE_DETAIL = "/autismtutor/student-detail/";
        public const string URL_FE_PARENT_TUTOR_REQUEST = "/autismedu/request-history";
        public const string URL_FE_PARENT_STUDENT_PROFILE_LIST = "/autismedu/my-tutor/";
        public const string URL_FE_PARENT_UPDATE_STATUS_STUDENT_PROFILE = "/autismedu/approve-student-profile/";
        public const string URL_FE_TUTOR_REVIEW_LIST = "/autismtutor/review-list";
        public const string URL_FE_TUTOR_TUTOR_REQUEST = "/autismtutor/tutor-request";
        public const string URL_FE_PAYMENT_QR = "/autismtutor/payment-package";

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

        // Const parameter 
        public const string AVAILABLE_TIME = "Thời gian rảnh";
        public const string TIME_SLOT = "Khung giờ";
        public const string ASSESSMENT_QUESTION = "Câu hỏi đánh giá";
        public const string BLOG = "Blog";
        public const string CERTIFICATE = "Chứng chỉ";
        public const string CLAIM = "Claim";
        public const string CURRICULUM = "Khung chương trình";
        public const string ID = "Id";
        public const string AGE = "Độ tuổi";
        public const string CHILD_INFO = "Thông tin trẻ";
        public const string CHILD_NAME = "Tên trẻ";
        public const string EXERCISE = "Bài tập";
        public const string EXERCISE_TYPE = "Loại bài tập";
        public const string PROGRESS_REPORT = "Sổ liên lạc";
        public const string REVIEW = "Review";
        public const string STUDENT_PROFILE = "Hồ sơ học sinh";
        public const string EMAIL = "Email";
        public const string PARENT = "Phụ huynh";
        public const string CHILD = "Trẻ";
        public const string STATUS_CHANGE = "Thay đổi trạng thái";
        public const string SYLLABUS = "Giáo trình";
        public const string TUTOR = "Gia sư";
        public const string TUTOR_REGISTRATION_REQUEST = "Đơn đăng ký làm gia sư";
        public const string TUTOR_REQUEST = "Đơn đăng ký học gia sư";
        public const string USER = "Người dùng";
        public const string WORK_EXPERIENCE = "Kinh nghiệm làm việc";
        public const string SCHEDULE = "Lịch học";
        public const string PASSWORD = "Mật khẩu";
        public const string ASSESSMENT_SCORE_RANGE = "Khoảng điểm đánh giá";
        public const string SCORE_RANGE = "Khoảng điểm";
        public const string END_TUTORING = "Đơn kết thúc dạy";
        public const string TEST = "Bài test";
        public const string QUESTION = "Câu hỏi";
        public const string OPTION = "Lựa chọn";
        public const string PACKAGE_PAYMENT = "Gói thanh toán";
		public const string TEST_RESULT = "Kết quả bài test";
		public const string NOTIFICATION = "Thông báo";
		public const string INFORMATION = "Thông tin";
		public const string APPLICATION_TOKEN = "Token hệ thống";
		public const string GOOGLE_TOKEN = "Token Google";
		public const string GOOGLE_REFRESH_TOKEN_STRING = "Refresh Token Google";
		public const string ROLE = "Vai trò";




        // Const message 
        public const string TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR = "TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR";
        public const string EMAIL_EXISTING_MESSAGE = "EMAIL_EXISTING_MESSAGE";
        public const string TUTOR_UPDATE_STATUS_IS_PENDING = "TUTOR_UPDATE_STATUS_IS_PENDING";
        public const string BAD_REQUEST_MESSAGE = "BAD_REQUEST_MESSAGE";
        public const string DATA_DUPLICATED_MESSAGE = "DATA_DUPLICATED_MESSAGE";
        public const string NOT_FOUND_MESSAGE = "NOT_FOUND_MESSAGE";
        public const string TIMESLOT_DUPLICATED_MESSAGE = "TIMESLOT_DUPLICATED_MESSAGE";
        public const string INTERNAL_SERVER_ERROR_MESSAGE = "INTERNAL_SERVER_ERROR_MESSAGE";
        public const string REVIEW_DELETE_SUCCESS = "Xoá đánh giá thành công!";
        public const string BAD_ACTION_REVIEW = "BAD_ACTION_REVIEW";
        public const string MISSING_INFORMATION = "MISSING_INFORMATION";
        public const string MISSING_2_INFORMATIONS = "MISSING_2_INFORMATIONS";
        public const string STUDENT_PROFILE_EXPIRED = "STUDENT_PROFILE_EXPIRED";
        public const string CANNOT_ADD_ROLE = "CANNOT_ADD_ROLE";
        public const string USER_HAVE_NO_ROLE = "USER_HAVE_NO_ROLE";
        public const string DUPPLICATED_ASSIGN_EXERCISE = "DUPPLICATED_ASSIGN_EXERCISE";
        public const string ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE = "ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE";
        public const string EMAIL_NOT_CONFIRM = "EMAIL_NOT_CONFIRM";
        public const string LOGIN_WRONG_SIDE = "LOGIN_WRONG_SIDE";
        public const string GG_CANNOT_CHANGE_PASSWORD = "GG_CANNOT_CHANGE_PASSWORD";
        public const string CHANGE_PASS_FAIL = "CHANGE_PASS_FAIL";
        public const string ADD_ROLE_USER_TO_USER = "ADD_ROLE_USER_TO_USER";
        public const string PROGRESS_REPORT_MODIFICATION_EXPIRED = "PROGRESS_REPORT_MODIFICATION_EXPIRED";
        public const string NEED_PAYMENT_MESSAGE = "NEED_PAYMENT_MESSAGE";
        public const string LINK_EXPIRED_MESSAGE = "LINK_EXPIRED_MESSAGE";
        public const string GOOGLE_USER_INVALID_FORGOT_PASSWORD_MESSAGE = "GOOGLE_USER_INVALID_FORGOT_PASSWORD_MESSAGE";
        public const string ACCOUNT_IS_LOCK_MESSAGE = "ACCOUNT_IS_LOCK_MESSAGE";
        public const string USERNAME_PASSWORD_INVALID_MESSAGE = "USERNAME_PASSWORD_INVALID_MESSAGE";
        public const string REGISTER_FAILED_MESSAGE = "REGISTER_FAILED_MESSAGE";
        public const string REFRESH_TOKEN_ERROR_MESSAGE = "REFRESH_TOKEN_ERROR_MESSAGE";
        public const string TOKEN_EXPIRED_MESSAGE = "TOKEN_EXPIRED_MESSAGE";

        // const notification
        public const string CREATE_STUDENT_PROFILE_PARENT_NOTIFICATION = "CREATE_STUDENT_PROFILE_PARENT_NOTIFICATION";
        public const string UPDATE_PROGRESS_REPORT_PARENT_NOTIFICATION = "UPDATE_PROGRESS_REPORT_PARENT_NOTIFICATION";
        public const string CREATE_PROGRESS_REPORT_PARENT_NOTIFICATION = "CREATE_PROGRESS_REPORT_PARENT_NOTIFICATION";
        public const string CHANGE_STATUS_STUDENT_PROFILE_TUTOR_NOTIFICATION = "CHANGE_STATUS_STUDENT_PROFILE_TUTOR_NOTIFICATION";
        public const string CHANGE_STATUS_CURRICULUM_TUTOR_NOTIFICATION = "CHANGE_STATUS_CURRICULUM_TUTOR_NOTIFICATION";
        public const string CHANGE_STATUS_CERTIFICATE_TUTOR_NOTIFICATION = "CHANGE_STATUS_CERTIFICATE_TUTOR_NOTIFICATION";
        public const string CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION = "CHANGE_STATUS_STUDENT_PROFILE_NOTIFICATION";
        public const string TUTOR_REQUEST_TUTOR_NOTIFICATION = "TUTOR_REQUEST_TUTOR_NOTIFICATION";
        public const string NEW_REVIEW_TUTOR_NOTIFICATION = "NEW_REVIEW_TUTOR_NOTIFICATION";


        // DTO require message
        public const string WEEKDAY_REQUIRED = "Chưa nhập ngày trong tuần";
        public const string TIMESLOT_REQUIRED = "Chưa nhập khung giờ";
        public const string OPTION_TEXT_REQUIRED = "Chưa nhập đáp án";
        public const string POINT_REQUIRED = "Chưa nhập điểm";
        public const string QUESTION_REQUIRED = "Chưa nhập câu hỏi";
        public const string DESCRIPTION_REQUIRED = "Chưa nhập mô tả";
        public const string TITLE_REQUIRED = "Chưa nhập tiêu đề";
        public const string CONTENT_REQUIRED = "Chưa nhập nội dung";
        public const string CERTIFICATE_NAME_REQUIRED = "Chưa nhập tên chứng chỉ";
        public const string NAME_REQUIRED = "Chưa nhập tên";
        public const string GENDER_REQUIRED = "Chưa nhập giới tính";
        public const string BIRTH_DATE_REQUIRED = "Chưa nhập ngày sinh";
        public const string CLAIM_TYPE_REQUIRED = "Chưa nhập claim type";
        public const string CLAIM_VALUE_REQUIRED = "Chưa nhập claim value";
        public const string AGE_REQUIRED = "Chưa nhập tuổi";
        public const string DATE_REQUIRED = "Chưa nhập ngày";
        public const string ID_REQUIRED = "Chưa nhập Id";
        public const string PRICE_REQUIRED = "Chưa nhập giá";
        public const string PHONE_NUMBER_REQUIRED = "Chưa nhập số điện thoại";
        public const string ADDRESS_REQUIRED = "Chưa nhập địa chỉ";
        public const string EMAIL_REQUIRED = "Chưa nhập email";
        public const string SESSION_HOUR_REQUIRED = "Chưa nhập thời gian một tiết học";
        public const string POSITION_REQUIRED = "Chưa nhập vị trí";
        public const string TEST_RESULT_REQUIRED = "Chưa nhập kết quả bài test";
        public const string TOTAL_POINT_REQUIRED = "Chưa nhập tổng điểm";

        // const exercise type
        public const string DEFAULT_EXERCISE_TYPE_1 = "Tập phát âm thuở ban đầu - nhưng âm thanh của trẻ nhỏ";
        public const string DEFAULT_EXERCISE_TYPE_2 = "Tập phát âm thuở ban đầu - lời nói đầu tiên";
        public const string DEFAULT_EXERCISE_TYPE_3 = "Nghe: Chú ý - nhận biết các âm";
        public const string DEFAULT_EXERCISE_TYPE_4 = "Nghe: Chú ý - tìm kiếm và dõi theo các âm thanh";
        public const string DEFAULT_EXERCISE_TYPE_5 = "Nghe: Chú ý - đáp lại sự chú ý bằng cách mỉm cười và phát ra âm thanh";
        public const string DEFAULT_EXERCISE_TYPE_6 = "Nghe: Chú ý - làm cho người khác phải chú ý đến mình";
        public const string DEFAULT_EXERCISE_TYPE_7 = "Bắt chước - các cử chỉ (lần lượt - con làm gì đó và bố/ mẹ sẽ làm việc gì đó)";
        public const string DEFAULT_EXERCISE_TYPE_8 = "Bắt chước - âm thanh";
        public const string DEFAULT_EXERCISE_TYPE_9 = "Đáp lại lời nói";
        public const string DEFAULT_EXERCISE_TYPE_10 = "Những tiếng nói đầu tiên - những từ chỉ đồ vật";
        public const string DEFAULT_EXERCISE_TYPE_11 = "Những tiếng nói đầu tiên - những cấu trúc đầu tiên";
        public const string DEFAULT_EXERCISE_TYPE_12 = "Nghe - Chú ý";
        public const string DEFAULT_EXERCISE_TYPE_13 = "Bắt chước";
        public const string DEFAULT_EXERCISE_TYPE_14 = "Chơi với truyện tranh";
        public const string DEFAULT_EXERCISE_TYPE_15 = "Đáp lại ngôn ngữ";
        public const string DEFAULT_EXERCISE_TYPE_16 = "Những tiếng nói đầu tiên - động từ, tính từ và cụm có hai từ";
        public const string DEFAULT_EXERCISE_TYPE_17 = "Những tiếng nói đầu tiên - câu hỏi";
        public const string DEFAULT_EXERCISE_TYPE_18 = "Những tiếng nói đầu tiên - cấu trúc câu";
        public const string DEFAULT_EXERCISE_TYPE_19 = "Lắng nghe - Chú ý";
        public const string DEFAULT_EXERCISE_TYPE_20 = "Bắt chước";
        public const string DEFAULT_EXERCISE_TYPE_21 = "Chơi với các quyển sách tranh";
        public const string DEFAULT_EXERCISE_TYPE_22 = "Đáp lại lời nói của người khác";
        public const string DEFAULT_EXERCISE_TYPE_23 = "Lời nói ban đầu - Giao tiếp";
        public const string DEFAULT_EXERCISE_TYPE_24 = "Những lời nói đầu tiên - sử dụng các khái niệm nhận thức";
        public const string DEFAULT_EXERCISE_TYPE_25 = "Những tiếng nói đầu tiên - Câu hỏi";
        public const string DEFAULT_EXERCISE_TYPE_26 = "Những tiếng nói đầu tiên - Cấu trúc câu";
        public const string DEFAULT_EXERCISE_TYPE_27 = "Lắng nghe - Chú ý";
        public const string DEFAULT_EXERCISE_TYPE_28 = "Bắt chước";
        public const string DEFAULT_EXERCISE_TYPE_29 = "Trò chơi và sách tranh";
        public const string DEFAULT_EXERCISE_TYPE_30 = "Đáp ứng ngôn ngữ";

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
            NOT_YET = 2,
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
            Pending = 3,
            Reject = 2,
            Teaching = 1,
            Stop = 0
        }

        public const string IncompatibilityWithCurriculumMsg = "Không tương thích với chương trình giảng dạy";
        public const string SchedulingConflictsMsg = "Xung đột lịch trình";
        public const string REQUEST_TIMEOUT_EXPIRED = "Thời gian yêu cầu hết hạn";
        public const string OtherMsg = "Khác";


        // Status string
        public const string STATUS_PENDING = "pending";
        public const string STATUS_APPROVE = "approve";
        public const string STATUS_REJECT = "reject";
        public const string STATUS_ALL = "all";

        public const string STATUS_APPROVE_VIE = "được chấp nhận";
        public const string STATUS_REJECT_VIE = "bị từ chối";

        // sort Order
        public const string ORDER_DESC = "desc";
        public const string ORDER_ASC = "asc";

        // Order by
        public const string CREATED_DATE = "createdDate";
        public const string PUBLISH_DATE = "publishDate";
        public const string AGE_FROM = "ageFrom";
        public const string DATE_FROM = "dateFrom";
        public const string DATE_TO = "dateTo";
    }
}
