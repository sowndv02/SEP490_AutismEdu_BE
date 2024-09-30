import DashBoard from '~/pages/Admin/DashBoard'
import RoleClaimManagement from '~/pages/Admin/RoleClaimManagement'
import UserManagement from '~/pages/Admin/UserManagement'
import ConfirmRegister from '~/pages/Auth/ConfirmRegister'
import ForgotPassword from '~/pages/Auth/ForgotPassword'
import Login from '~/pages/Auth/Login'
import Register from '~/pages/Auth/Register'
import ResetPassword from '~/pages/Auth/ResetPassword'
import CenterProfile from '~/pages/Center/CenterProfile'
import SearchCenter from '~/pages/Center/SearchCenter'
import Home from '~/pages/Home'
import PAGES from '~/utils/pages'
import SearchTutor from '~/pages/Tutor/SearchTutor'
import TutorProfile from '~/pages/Tutor/TutorProfile'
import TutorRegistrationManagement from '~/pages/Admin/TutorRegistrationManagement'

const publicRoutes = [
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
        path: PAGES.ROOT + PAGES.LISTCENTER,
        element: SearchCenter
    },
    {
        path: PAGES.ROOT + PAGES.CENTERPROFILE,
        element: CenterProfile
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
        path: PAGES.ROOT + PAGES.TUTORREGISTRATION,
        element: TutorRegistration
    },
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
    }
]
export {
    adminRoutes, publicRoutes
}
