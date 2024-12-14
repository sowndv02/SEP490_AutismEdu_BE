import { Box, Tab } from '@mui/material';
import { TabContext, TabList, TabPanel } from '@mui/lab';
import React, { useState, useEffect } from 'react';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import LocalLibraryIcon from '@mui/icons-material/LocalLibrary';
import ExerciseTypeList from './ExerciseTypeList';
import SyllabusManagement from './SyllabusManagement';
import { useLocation } from 'react-router-dom';

function ExerciseManagement() {
    const location = useLocation();
    const [value, setValue] = useState(() => {
        const syllabus = location.state?.syllabus;
        return syllabus ?? '1';
    });

    const handleChange = (event, newValue) => {
        setValue(newValue);
    };
    return (
        <Box sx={{ width: '100%', typography: 'body1' }}>
            <TabContext value={value}>
                <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                    <TabList onChange={handleChange} aria-label="lab API tabs example">
                        <Tab iconPosition='start' icon={<MenuBookIcon />} label="Bài tập" value="1" />
                        <Tab iconPosition='start' icon={<LocalLibraryIcon />} label="Giáo trình" value="2" />
                    </TabList>
                </Box>
                <TabPanel value="1">
                    <ExerciseTypeList />
                </TabPanel>
                <TabPanel value="2">
                    <SyllabusManagement />
                </TabPanel>

            </TabContext>
        </Box>
    )
}

export default ExerciseManagement
