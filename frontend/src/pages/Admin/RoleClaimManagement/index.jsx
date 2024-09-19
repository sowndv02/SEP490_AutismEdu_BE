<<<<<<< HEAD
import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import ClaimTable from './ClaimManagement/ClaimTable'
import { useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
=======
import { Box } from '@mui/material';
import LoadingComponent from '~/components/LoadingComponent';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
import ClaimManagement from './ClaimManagement';
import RoleManagement from './RoleManagement';
function RoleClaimManagement() {
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
<<<<<<< HEAD
            marginTop: (theme) => theme.myapp.adminHeaderHeight
        }}>
            <ClaimManagement />
            <RoleManagement />
=======
            marginTop: (theme) => theme.myapp.adminHeaderHeight,
            position: 'relative'
        }}>
            <ClaimManagement />
            <RoleManagement />
            <LoadingComponent />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        </Box>
    )
}

export default RoleClaimManagement
