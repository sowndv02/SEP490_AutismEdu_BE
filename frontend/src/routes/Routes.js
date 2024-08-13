import PAGES from '~/utils/pages'
import Login from '~/pages/Auth/Login'
import Home from '~/pages/Home'
import DashBoard from '~/pages/Admin/DashBoard'
import UserManagement from '~/pages/Admin/UserManagement'
import RoleClaimManagement from '~/pages/Admin/RoleClaimManagement'
import ForgotPassword from '~/pages/Auth/ForgotPassword'

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
    }
]
export {
    publicRoutes,
    adminRoutes
}