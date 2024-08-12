import { Avatar, Box } from '@mui/material'
import React from 'react'

function Index() {
    return (
        <Box sx={{
            height: (theme) => `${theme.myapp.adminHeaderHeight}`,
            width: (theme) => `calc(100vw - ${theme.myapp.adminSideBarWidth})`,
            display: "flex",
            alignItems: "center",
            justifyContent: "end",
            position: "fixed",
            top: "0px",
            left: (theme) => theme.myapp.adminSideBarWidth,
            zIndex: "10",
            bgcolor: "#f7f7f9",
            px: "20px"
        }}>
            <Avatar alt="Remy Sharp" src="/static/images/avatar/1.jpg" />
        </Box>
    )
}

export default Index
