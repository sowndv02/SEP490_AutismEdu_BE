<<<<<<< HEAD
import PAGES from '~/utils/pages'
import Login from '~/pages/Auth/Login'
import Home from '~/pages/Home'
import DashBoard from '~/pages/Admin/DashBoard'
import UserManagement from '~/pages/Admin/UserManagement'
import RoleClaimManagement from '~/pages/Admin/RoleClaimManagement'
import ForgotPassword from '~/pages/Auth/ForgotPassword'
import Register from '~/pages/Auth/Register'
import ResetPassword from '~/pages/Auth/ResetPassword'
import ConfirmRegister from '~/pages/Auth/ConfirmRegister'
import SearchCenter from '~/pages/Center/SearchCenter'

const publicRoutes = [
    {
        path: PAGES.HOME,
        element: Home
    },
    {
        path: PAGES.LOGIN,
        element: Login
    },
    {
        path: PAGES.FORGOTPASSWORD,
        element: ForgotPassword
    },
    {
        path: PAGES.REGISTER,
        element: Register
    },
    {
        path: PAGES.RESETPASSWORD,
        element: ResetPassword
    },
    {
        path: PAGES.CONFIRMREGISTER,
        element: ConfirmRegister
    },
    {
        path: PAGES.LISTCENTER,
        element: SearchCenter
    },
=======
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
    }
]
export {
<<<<<<< HEAD
    publicRoutes,
    adminRoutes
}
=======
    adminRoutes, publicRoutes
}
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
