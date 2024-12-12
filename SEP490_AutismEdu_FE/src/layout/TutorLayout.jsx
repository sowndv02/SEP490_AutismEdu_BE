import { Box, Stack } from '@mui/material';
import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import TutorHeader from '~/components/TutorLayout/TutorHeader';
import TutorSideBar from '~/components/TutorLayout/TutorSideBar';
function TutorLayout() {
    const [openMenu, setOpenMenu] = useState(true);
    return (
        <Box>
            <TutorHeader setOpenMenu={setOpenMenu} openMenu={openMenu} />
            <Box marginTop={"65px"} component="main">
                <Stack direction="row">
                    <TutorSideBar openMenu={openMenu} />
                    <Outlet />
                </Stack>
            </Box>
        </Box>
    )
}

export default TutorLayout
