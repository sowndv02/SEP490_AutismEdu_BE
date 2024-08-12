import { Box } from '@mui/material'
import React from 'react'
import AdminLeftBar from '~/Components/AdminLeftBar'
import theme from '~/theme'
import AdminHeader from '~/Components/AdminHeader'
import UserContent from './UserContent'
function UserManagement() {
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
                <UserContent />
            </Box>
        </Box>
    )
}

export default UserManagement
