import { Box } from '@mui/material';
import LoadingComponent from '~/components/LoadingComponent';
import ClaimManagement from './ClaimManagement';
import RoleManagement from './RoleManagement';
function RoleClaimManagement() {
    return (
        <Box sx={{
            px: 2,
            py: 2,
            position: 'relative'
        }}>
            {/* <ClaimManagement /> */}
            <RoleManagement />
            <LoadingComponent />
        </Box>
    )
}

export default RoleClaimManagement
