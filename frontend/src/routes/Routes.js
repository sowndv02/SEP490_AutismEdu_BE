import PAGES from '~/utils/pages'
import Login from '~/pages/Auth/Login'
import Home from '~/pages/Home'
import DashBoard from '~/pages/Admin/DashBoard'
import UserManagement from '~/pages/Admin/UserManagement'
import RoleClaimManagement from '~/pages/Admin/RoleClaimManagement'

const publicRoutes = [
    {
        path: PAGES.HOME,
        element: Home
    },
    {
        path: PAGES.LOGIN,
        element: Login
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