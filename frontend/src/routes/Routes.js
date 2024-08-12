import PAGES from '~/utils/pages'
import Login from '~/pages/Auth/Login'
import Home from '~/pages/Home'
import DashBoard from '~/pages/Admin/DashBoard'

const routes = [
    {
        path: PAGES.HOME,
        element: Home
    },
    {
        path: PAGES.LOGIN,
        element: Login
    },
    {
        path: PAGES.DASHBOARD,
        element: DashBoard
    }
]

export default {
    routes
}