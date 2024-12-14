import { Box, Stack } from '@mui/material'
import { Outlet } from 'react-router-dom';
import AdminHeader from '~/components/AdminHeader';
import AdminLeftBar from '~/components/AdminLeftBar';
function AdminLayout() {
    return (
        <Box>
            <AdminHeader />
            <Box marginTop={"65px"} sx={{ height: "calc(100vh - 65px)" }} component="main">
                <Stack direction="row" sx={{ bgcolor: "#f5f5f9", height: "100%" }}>
                    <Box width={"17%"} sx={{ height: "100%", overflow: "auto" }}>
                        <AdminLeftBar />
                    </Box>
                    <Box width={"83%"} padding={3} sx={{ height: "100%", overflow: "auto" }}>
                        <Outlet />
                    </Box>
                </Stack>
            </Box >
        </Box >
    )
}

export default AdminLayout
