import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import ClaimTable from './ClaimManagement/ClaimTable'
import { useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import ClaimManagement from './ClaimManagement';
import RoleManagement from './RoleManagement';
function RoleClaimManagement() {
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
            marginTop: (theme) => theme.myapp.adminHeaderHeight
        }}>
            <ClaimManagement />
            <RoleManagement />
        </Box>
    )
}

export default RoleClaimManagement
