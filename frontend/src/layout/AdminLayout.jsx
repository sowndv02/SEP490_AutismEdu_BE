import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import AdminHeader from '~/components/AdminHeader';
import AdminLeftBar from '~/components/AdminHeader';
function AdminLayout() {
    return (
        <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", gap: "2" }}>
            <AdminLeftBar />
            <Box sx={{
                width: (theme) => `calc(100vw - ${theme.myapp.adminSideBarWidth})`,
                height: "100vh",
                px: "20px",
                overflow: "auto"
            }}>
                <AdminHeader />
                <Outlet />
            </Box>
        </Box>
    )
}

export default AdminLayout
