import AccountBoxOutlinedIcon from '@mui/icons-material/AccountBoxOutlined';
import BarChartIcon from '@mui/icons-material/BarChart';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import NoteAltOutlinedIcon from '@mui/icons-material/NoteAltOutlined';
import { Box, Divider, Tab, Tabs, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import services from '~/plugins/services';
import AssessmentChart from './AssessmentChart';
import ProgressReport from './ProgressReport';
import StudentInformation from './StudentInformation';
import ScheduleSetting from './ScheduleSetting';
import ManageHistoryIcon from '@mui/icons-material/ManageHistory';
import StudentExcercise from './StudentExcercise';
import AutoStoriesIcon from '@mui/icons-material/AutoStories';
import StudentSchedule from './StudentSchedule';
function StudentDetail() {
    const [tab, setTabs] = useState(0);
    const { id } = useParams();
    const [studentProfile, setStudentProfile] = useState(null);
    const handleChange = (event, newValue) => {
        setTabs(newValue);
    };

    useEffect(() => {
        handleGetStudentProfile();
    }, [])

    useEffect(() => {
        handleGetStudentProfile();
        setTabs(0)
    }, [id])
    const handleGetStudentProfile = async () => {
        try {
            await services.StudentProfileAPI.getStudentProfileById(id, (res) => {
                setStudentProfile(res.result)
            }, (error) => {
                console.log(error);
            }, {
                status: "Teaching"
            })
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Box sx={{
            flexGrow: 2, height: "calc(100vh - 65px)",
            overflow: "auto",
        }}>
            <Box px={3} sx={{ display: 'flex', alignItems: "center", justifyContent: "space-between" }}>
                <Tabs
                    value={tab}
                    onChange={handleChange}
                    aria-label="icon position tabs example"
                >
                    <Tab icon={<CalendarMonthOutlinedIcon />} iconPosition="end" label="Lịch học" />
                    <Tab icon={<NoteAltOutlinedIcon />} iconPosition="end" label="Sổ liên lạc" />
                    <Tab icon={<BarChartIcon />} iconPosition="end" label="Biểu đồ đánh giá" />
                    <Tab icon={<AutoStoriesIcon />} iconPosition="end" label="Bài tập" />
                    <Tab icon={<ManageHistoryIcon />} iconPosition="end" label="Cài đặt lịch học" />
                    <Tab icon={<AccountBoxOutlinedIcon />} iconPosition="end" label="Thông tin học sinh" />
                </Tabs>
                <Box sx={{ maxWidth: "200px" }}>
                    <Typography sx={{
                        fontWeight: "bold", color: "#b660ec", fontSize: "20px",
                        maxWidth: "200px", overflow:"hidden", height:"25px"
                    }}
                    >{studentProfile?.name} - {studentProfile?.studentCode}</Typography>
                    {
                        studentProfile?.status === 1 && (
                            <Typography sx={{ color: "blue" }}>Đang học</Typography>
                        )
                    }
                    {
                        studentProfile?.status === 0 && (
                            <Typography>Đã kết thúc</Typography>
                        )
                    }
                </Box>
            </Box>
            <Divider sx={{ width: "100%" }} />
            {
                tab === 0 && studentProfile?.status !== 3 && <StudentSchedule studentProfile={studentProfile} />
            }
            {
                tab === 1 && studentProfile?.status !== 3 && <ProgressReport studentProfile={studentProfile} />
            }
            {
                tab === 2 && studentProfile?.status !== 3 && <AssessmentChart studentProfile={studentProfile} />
            }
            {
                tab === 3 && studentProfile?.status !== 3 && <StudentExcercise studentProfile={studentProfile} />
            }
            {
                tab === 4 && studentProfile?.status !== 3 && <ScheduleSetting studentProfile={studentProfile} />
            }
            {
                tab === 5 && <StudentInformation studentProfile={studentProfile} setStudentProfile={setStudentProfile} />
            }
        </Box>
    )
}

export default StudentDetail
