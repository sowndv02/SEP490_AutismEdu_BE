import { Stack, Typography } from '@mui/material'
import React from 'react'
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import { AppBar, Tabs, Tab } from '@mui/material';
function Logo({ sizeLogo, sizeName }) {
    return (
        <Stack direction="row" sx={{ alignItems: "center" }}>
            <EscalatorWarningIcon sx={{ fontSize: sizeLogo, color: "#394ef4" }} />
            <Typography sx={{
                fontSize: sizeName, fontWeight: "bold",
                background: 'linear-gradient(to right, #3c4ff4, #b660ec)',
                WebkitBackgroundClip: "text",
                color: "transparent",
            }}>AutismEdu</Typography>
        </Stack>
    )
}

export default Logo
