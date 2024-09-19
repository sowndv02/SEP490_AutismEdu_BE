import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
<<<<<<< HEAD
=======
import Footer from '~/components/Footer';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
import Header from '~/components/Header';
function ClientLayout() {
    return (
        <Box>
            <Header />
            <Box marginTop={"80px"}>
                <Outlet />
            </Box>
<<<<<<< HEAD
=======
            <Footer />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        </Box>
    )
}

export default ClientLayout
