import { Box } from '@mui/material';
import LoadingComponent from '~/components/LoadingComponent';
import ClaimManagement from './ClaimManagement';
import RoleManagement from './RoleManagement';
function RoleClaimManagement() {
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
            marginTop: (theme) => theme.myapp.adminHeaderHeight,
            position: 'relative'
        }}>
            <ClaimManagement />
            <RoleManagement />
            <LoadingComponent />
        </Box>
    )
}

export default RoleClaimManagement
