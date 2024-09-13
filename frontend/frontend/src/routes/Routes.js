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
        path: '/',
        element: Home
    },
    {
        path: '/login',
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