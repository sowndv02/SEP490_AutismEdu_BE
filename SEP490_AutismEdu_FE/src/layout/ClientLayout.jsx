import { Box } from '@mui/material';
import { useEffect } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import Footer from '~/components/Footer';
import Header from '~/components/Header';
function ClientLayout() {
    const { pathname } = useLocation();

    useEffect(() => {
        window.scrollTo(0, 0);
    }, [pathname]);
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
