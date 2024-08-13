import Login from '~/pages/Auth/Login'
import Home from '~/pages/Home'
import DashBoard from '~/pages/Admin/DashBoard'
import UserManagement from '~/pages/Admin/UserManagement'

const publicRoutes = [
    {
        path: '/',
        element: Home
    },
    {
        path: '/login',
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
    }
]
export {
    publicRoutes,
    adminRoutes
}