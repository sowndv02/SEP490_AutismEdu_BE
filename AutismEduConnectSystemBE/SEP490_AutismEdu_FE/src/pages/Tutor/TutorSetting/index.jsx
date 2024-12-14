import { Box, Tab } from '@mui/material';
import { useNavigate, useLocation } from 'react-router-dom';
import { TabContext, TabList, TabPanel } from '@mui/lab';
import React, { useState, useEffect } from 'react';
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts';
import AutoStoriesIcon from '@mui/icons-material/AutoStories';
import EditCalendarIcon from '@mui/icons-material/EditCalendar';
import DescriptionIcon from '@mui/icons-material/Description';
import WorkIcon from '@mui/icons-material/Work';
import EditProfile from './EditProfile';
import CurriculumManage from './CurriculumManagement';
import AvailableTimeManagement from './AvailableTimeManagement';
import CertificateManagement from './CertificateManagement';
import WorkExperienceManagement from './WorkExperienceManagement';
import ReviewsIcon from '@mui/icons-material/Reviews';
import ReviewMe from './ReviewMe';

function TutorSetting() {
    const navigate = useNavigate();
    const location = useLocation();

    const initialTab = location.state?.selectedTab || '1';
    const [value, setValue] = useState(initialTab);

    const handleChange = (event, newValue) => {
        setValue(newValue);
        navigate('.', { state: { selectedTab: newValue } });
    };

    return (
        <Box sx={{ width: '100%', typography: 'body1', height: "calc(100vh - 65px)", overflow: "auto" }}>
            <TabContext value={value}>
                <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                    <TabList onChange={handleChange} aria-label="lab API tabs example">
                        <Tab iconPosition='start' icon={<ManageAccountsIcon />} label="Chỉnh sửa hồ sơ" value="1" />
                        <Tab iconPosition='start' icon={<AutoStoriesIcon />} label="Khung chương trình" value="2" />
                        <Tab iconPosition='start' icon={<EditCalendarIcon />} label="Thiết lập thời gian rảnh" value="3" />
                        <Tab iconPosition='start' icon={<DescriptionIcon />} label="Chứng chỉ" value="4" />
                        <Tab iconPosition='start' icon={<WorkIcon />} label="Kinh nghiệm làm việc" value="5" />
                        <Tab iconPosition='start' icon={<ReviewsIcon />} label="Đánh giá về tôi" value="6" />
                    </TabList>
                </Box>
                <TabPanel value="1">
                    <EditProfile />
                </TabPanel>
                <TabPanel value="2"><CurriculumManage /></TabPanel>
                <TabPanel value="3"><AvailableTimeManagement /></TabPanel>
                <TabPanel value="4"><CertificateManagement /></TabPanel>
                <TabPanel value='5'><WorkExperienceManagement /></TabPanel>
                <TabPanel value='6'><ReviewMe /></TabPanel>
            </TabContext>
        </Box>
    );
}

export default TutorSetting;
