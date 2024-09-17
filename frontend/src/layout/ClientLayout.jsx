import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import Footer from '~/components/Footer';
import Header from '~/components/Header';
function ClientLayout() {
    return (
        <Box>
            <Header />
            <Box marginTop={"80px"}>
                <Outlet />
            </Box>
            <Footer />
        </Box>
    )
}

export default ClientLayout
