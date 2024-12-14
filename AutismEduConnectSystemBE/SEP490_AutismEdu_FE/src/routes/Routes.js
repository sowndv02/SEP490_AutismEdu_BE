import RechargeModal from '~/components/PaymentModal/RechargeModal'
import AssessmentManagement from '~/pages/Admin/AssessmentManagement'
import AssessmentCreation from '~/pages/Admin/AssessmentManagement/AssessmentCreation'
import AssessmentScoreRange from '~/pages/Admin/AssessmentManagement/AssessmentScoreRange'
import BlogManagement from '~/pages/Admin/BlogManagement'
import BlogCreation from '~/pages/Admin/BlogManagement/BlogCreation'
import BlogUpdate from '~/pages/Admin/BlogManagement/BlogUpdate'
import DashBoard from '~/pages/Admin/DashBoard'
import ExerciseTypeManagement from '~/pages/Admin/ExerciseTypeManagement'
import LoginAdmin from '~/pages/Admin/LoginAdmin'
import PaymentHistoryAdmin from '~/pages/Admin/PaymentManagement/PaymentHistory'
import PaymentPackage from '~/pages/Admin/PaymentManagement/PaymentPackageManagement'
import ReportTutorManagement from '~/pages/Admin/ReportManagement/ReportTutorManagement'
import ReportDetail from '~/pages/Admin/ReportManagement/ReportTutorManagement/ReportDetail'
import RoleClaimManagement from '~/pages/Admin/RoleClaimManagement'
import TutorRegistrationManagement from '~/pages/Admin/TutorRegistrationManagement'
import TutorRegistrationDetail from '~/pages/Admin/TutorRegistrationManagement/TutorRegistrationDetail'
import CertificateManagement from '~/pages/Admin/TutorSettingManagement/CertificateManagement'
import CurriculumManagement from '~/pages/Admin/TutorSettingManagement/CurriculumManagement'
import UpdateRequest from '~/pages/Admin/TutorSettingManagement/UpdateRequest'
import WorkExperienceManagement from '~/pages/Admin/TutorSettingManagement/WorkExperienceManagement'
import UserManagement from '~/pages/Admin/UserManagement'
import ConfirmRegister from '~/pages/Auth/ConfirmRegister'
import ForgotPassword from '~/pages/Auth/ForgotPassword'
import Login from '~/pages/Auth/Login'
import TutorLogin from '~/pages/Auth/Login/LoginForm/TutorLogin'
import LoginOption from '~/pages/Auth/Login/LoginOption'
import Register from '~/pages/Auth/Register'
import RegisterOptions from '~/pages/Auth/Register/RegisterOptions'
import ResetPassword from '~/pages/Auth/ResetPassword'
import Blog from '~/pages/Blog'
import BlogDetail from '~/pages/Blog/BlogDetail'
import Home from '~/pages/Home'
import MyChildren from '~/pages/MyChildren'
import ChangePassword from '~/pages/Parent/ChangePassword'
import ParentProfile from '~/pages/Parent/ParentProfile'
import TutorRequestHistory from '~/pages/Parent/TutorRequestHistory/TutorRequestHistory'
import StudentCreation from '~/pages/StudentProfile/StudentCreation'
import StudentProfileApprove from '~/pages/StudentProfile/StudentProfileApprove'
import Calendar from '~/pages/Tutor/Calendar'
import ExerciseManagement from '~/pages/Tutor/ExerciseManagement'
import MyStudent from '~/pages/Tutor/MyStudent'
import StudentDetail from '~/pages/Tutor/MyStudent/StudentDetail/index.jsx'
import MyTutor from '~/pages/Tutor/MyTutor'
import MyTutorDetail from '~/pages/Tutor/MyTutor/MyTutorDetail'
import PaymentHistory from '~/pages/Tutor/PaymentHistory'
import SearchTutor from '~/pages/Tutor/SearchTutor'
import TutorProfile from '~/pages/Tutor/TutorProfile'
import TutorProfileUpdate from '~/pages/Tutor/TutorProfileUpdate'
import TutorRegistration from '~/pages/Tutor/TutorRegistration'
import TutorRequest from '~/pages/Tutor/TutorRequest'
import TutorSetting from '~/pages/Tutor/TutorSetting'
import CertificateDetail from '~/pages/Tutor/TutorSetting/CertificateManagement/CertificateDetail'
import PAGES from '~/utils/pages'

import ParentTutorManagement from '~/pages/Admin/ParentTutorManagement'
import AdminParentProfile from '~/pages/Admin/ParentTutorManagement/ParentProfile'
import AdminTutorProfile from '~/pages/Admin/ParentTutorManagement/TutorProfile'
import ReportReviewManagement from '~/pages/Admin/ReportManagement/ReportReviewManagement'
import ReportReviewDetail from '~/pages/Admin/ReportManagement/ReportReviewManagement/ReportReviewDetail'

import ChangePasswordTutor from '~/pages/Tutor/TutorSetting/ChangePassword'
import AssessmentGuild from '~/pages/Tutor/AssessmentsGuild'
import AssessmentGuildClient from '~/pages/Tutor/AssessmentsGuild/AssessmentGuildClient'

const UnLayoutRoutes = [
    {
        path: PAGES.TUTOR_LOGIN,
        element: TutorLogin
    },
    {
        path: PAGES.TUTORREGISTRATION,
        element: TutorRegistration
    },
    {
        path: PAGES.LOGIN_ADMIN,
        element: LoginAdmin
    }
]

const publicRoutes = [
    {
        path: PAGES.ROOT + "/",
        element: Home
    },
    {
        path: PAGES.ROOT + PAGES.HOME,
        element: Home
    },
    {
        path: PAGES.ROOT + PAGES.LOGIN,
        element: Login
    },
    {
        path: PAGES.ROOT + PAGES.FORGOTPASSWORD,
        element: ForgotPassword
    },
    {
        path: PAGES.ROOT + PAGES.REGISTER,
        element: Register
    },
    {
        path: PAGES.ROOT + PAGES.RESETPASSWORD,
        element: ResetPassword
    },
    {
        path: PAGES.ROOT + PAGES.CONFIRMREGISTER,
        element: ConfirmRegister
    },
    {
        path: PAGES.ROOT + PAGES.LISTTUTOR,
        element: SearchTutor
    },
    {
        path: PAGES.ROOT + PAGES.TUTORPROFILE,
        element: TutorProfile
    },
    {
        path: PAGES.ROOT + PAGES.TUTORPROFILEUPDATE,
        element: TutorProfileUpdate
    },
    {
        path: PAGES.ROOT + PAGES.LOGIN_OPTION,
        element: LoginOption
    },
    {
        path: PAGES.ROOT + PAGES.REGISTER_OPTION,
        element: RegisterOptions
    },
    {
        path: PAGES.ROOT + PAGES.MY_CHILDREN,
        element: MyChildren
    },
    {
        path: PAGES.ROOT + PAGES.PARENT_PROFILE,
        element: ParentProfile
    },
    {
        path: PAGES.ROOT + PAGES.CHANGE_PASSWORD,
        element: ChangePassword
    },
    {
        path: PAGES.ROOT + PAGES.APPROVE_STUDENT_PROFILE,
        element: StudentProfileApprove
    },
    {
        path: PAGES.ROOT + PAGES.MY_TUTOR,
        element: MyTutor
    },
    {
        path: PAGES.ROOT + PAGES.MY_TUTOR_DETAIL,
        element: MyTutorDetail
    },
    {
        path: PAGES.ROOT + PAGES.TUTOR_REQUEST_HISTORY,
        element: TutorRequestHistory
    },
    {
        path: PAGES.ROOT + PAGES.BLOG_LIST,
        element: Blog
    },
    {
        path: PAGES.ROOT + PAGES.BLOG_DETAIL,
        element: BlogDetail
    },
    {
        path: PAGES.ROOT + PAGES.ASSESSMENT_GUILD_CLIENT,
        element: AssessmentGuildClient
    }
]


const tutorRoutes = [
    {
        path: PAGES.MY_STUDENT,
        element: MyStudent
    }, {
        path: PAGES.STUDENT_DETAIL,
        element: StudentDetail
    }, 
    {
        path: PAGES.TUTOR_SETTING,
        element: TutorSetting
    },
    {
        path: PAGES.TUTOR_REQUEST,
        element: TutorRequest
    }, {
        path: PAGES.CALENDAR,
        element: Calendar
    }, {
        path: PAGES.CERTIFICATE_DETAIL,
        element: CertificateDetail
    }, {
        path: PAGES.STUDENT_CREATION,
        element: StudentCreation
    }, {
        path: PAGES.EXERCISE_MANAGEMENT,
        element: ExerciseManagement
    }, {
        path: PAGES.PAYMENT_PACKAGE,
        element: RechargeModal
    }, {
        path: PAGES.PAYMENT_HISTORY_TUTOR,
        element: PaymentHistory
    }, {
        path: PAGES.CHANGE_PASSWORD_TUTOR,
        element: ChangePasswordTutor
    },
    {
        path: PAGES.ASSESSMENT_GUILD,
        element: AssessmentGuild
    }
]
const adminRoutes = [
    {
        path: PAGES.DASHBOARD,
        element: DashBoard
    },
    {
        path: PAGES.USERMANAGEMENT,
        element: UserManagement
    },
    {
        path: PAGES.ROLECLAIMMANAGEMENT,
        element: RoleClaimManagement
    },
    {
        path: PAGES.TUTORREGISTRATIONMANAGEMENT,
        element: TutorRegistrationManagement
    },
    {
        path: PAGES.TUTOR_REGISTRATION_DETAIL,
        element: TutorRegistrationDetail
    },
    {
        path: PAGES.ASSESSMENT_MANAGEMENT,
        element: AssessmentManagement
    },
    {
        path: PAGES.ASSESSMENT_CREATION,
        element: AssessmentCreation
    },
    {
        path: PAGES.PAYMENT_PACKAGE_MANAGEMENT,
        element: PaymentPackage
    },
    {
        path: PAGES.SCORE_RANGE,
        element: AssessmentScoreRange
    },
    {
        path: PAGES.BLOG_MANAGEMENT,
        element: BlogManagement
    },
    {
        path: PAGES.BLOG_CREATION,
        element: BlogCreation
    },
    {
        path: PAGES.BLOG_EDIT,
        element: BlogUpdate
    },
    {
        path: PAGES.REPORT_TUTOR_MANAGEMENT,
        element: ReportTutorManagement
    },
    {
        path: PAGES.REPORT_TUTOR_DETAIL,
        element: ReportDetail
    }
    , {
        path: PAGES.EXERCISE_TYPE_MANAGEMENT,
        element: ExerciseTypeManagement
    },
    {
        path: PAGES.PERSONAL_INFORMATION,
        element: UpdateRequest
    },
    {
        path: PAGES.CURRICULUM_MANAGEMENT,
        element: CurriculumManagement
    },
    {
        path: PAGES.CERTIFICATE_MANAGEMENT,
        element: CertificateManagement
    },
    {
        path: PAGES.WORK_EXPERIENCE_MANAGEMENT,
        element: WorkExperienceManagement
    },
    {
        path: PAGES.PAYMENT_HISTORY_ADMIN,
        element: PaymentHistoryAdmin
    },
    {
        path: PAGES.PARENT_TUTOR_MAMAGEMENT,
        element: ParentTutorManagement
    },
    {
        path: PAGES.ADMIN_TUTOR_PROFILE,
        element: AdminTutorProfile
    },
    {
        path: PAGES.ADMIN_PARENT_PROFILE,
        element: AdminParentProfile
    },
    {
        path: PAGES.ADMIN_REPORT_REVIEW,
        element: ReportReviewManagement
    },
    {
        path: PAGES.REPORT_REVIEW_DETAIL,
        element: ReportReviewDetail
    }

]
export { UnLayoutRoutes, adminRoutes, publicRoutes, tutorRoutes }

