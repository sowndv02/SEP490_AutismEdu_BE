import BusinessCenterOutlinedIcon from '@mui/icons-material/BusinessCenterOutlined';
import CakeOutlinedIcon from '@mui/icons-material/CakeOutlined';
import CancelIcon from '@mui/icons-material/Cancel';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ElevatorIcon from '@mui/icons-material/Elevator';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import PaidOutlinedIcon from '@mui/icons-material/PaidOutlined';
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined';
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined';
import StarIcon from '@mui/icons-material/Star';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import { Box, Button, Grid, IconButton, Stack, TextField, Typography } from '@mui/material';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import { useState } from 'react';
import { addDays, format, startOfWeek, addMinutes, isAfter } from 'date-fns';
import Reviews from './Reviews';
import EditAboutMe from './TutorProfileUpdateModal/EditAboutMe';

function TutorProfileUpdate() {
    const today = new Date();

    const [about, setAbout] = useState('Tên tôi là John Rotgers và tôi tốt nghiệp Thạc sĩ Quản trị Kinh doanh chuyên ngành Tiếp thị (Trực tuyến) tại Đại học Erasmus Rotterdam. Tôi là một chuyên gia chuyên về thương mại điện tử từ năm 2001. Tôi đã làm việc cho một số công ty quốc tế, như TNT Post và Centric. Ngoài ra, tôi còn làm tư vấn với tư cách là một nhà quản lý kinh doanh điện tử tự do. Tôi cũng đã thiết lập nhiều trang web và cửa hàng trực tuyến thành công trong lĩnh vực bán lẻ và du lịch. Tôi có thể giúp bạn với nhiều chủ đề liên quan đến kinh doanh điện tử và tiếp thị trực tuyến như viết văn bản, SEO, Google Ads và phân tích từ khóa và trang web.');
    const [learnGoal, setLearnGoal] = useState("<p>Con bạn sẽ:</br>* Học nhiều chiến lược đọc và cách áp dụng chúng.\n* Hiểu câu chuyện qua các câu hỏi hiểu bài.</br>* Luyện tập từ vựng nhìn thấy</br>* Luyện tập độ trôi chảy, từ vựng và nhận thức âm thanh.\n* Luyện tập lượt chơi, kỹ năng lắng nghe, kiên nhẫn và nhận thức xã hội.</br>* Luyện tập cấu trúc câu qua viết mô phỏng.</p>");
    const [additionalContent, setAdditionalContent] = useState(
        "Nội dung bổ sung:\n* Phát triển khả năng giao tiếp hiệu quả trong môi trường xã hội.\n* Học cách xử lý thông tin và giải quyết vấn đề qua câu chuyện.\n* Luyện tập trí nhớ thông qua các hoạt động liên quan đến từ vựng.\n* Nâng cao khả năng hiểu biết về ngữ pháp và cấu trúc câu.\n* Cải thiện sự tự tin và khả năng diễn đạt thông qua thảo luận nhóm."
    );

    const [value, setValue] = useState('1');
    const [valueCurriculum, setValueCurriculum] = useState('1');

    const handleChange = (event, newValue) => {
        setValue(newValue);
    };
    const handleChangeCurriculum = (event, newValue) => {
        setValueCurriculum(newValue);
    };

    const [schedule, setSchedule] = useState(format(today, 'yyyy-MM-dd'));
    const [availableTimes, setAvailableTimes] = useState([
        '8:30 – 9:00',
        '13:00 – 13:30'
    ]);
    const [startTime, setStartTime] = useState('');
    const [endTime, setEndTime] = useState('');
    console.log(startTime);
    
    const handleDateChange = (date) => {
        setSchedule(format(date, 'yyyy-MM-dd'));
    };

    const handleSave = () => {
        if (!startTime || !endTime) return; 

        const newTime = `${startTime} - ${endTime}`;

        const isDuplicate = availableTimes.some(time => time === newTime);
        if (isDuplicate) {
            alert('Khoảng thời gian đã tồn tại!');
            return;
        }

        setAvailableTimes((prevTimes) => [...prevTimes, newTime]);

        setStartTime('');
        setEndTime('');
    };

    const handleDeleteTime = (timeToDelete) => {
        setAvailableTimes((prevTimes) => prevTimes.filter(time => time !== timeToDelete));
    };

    const renderWeekButtons = () => {
        const startOfWeekDate = startOfWeek(today, { weekStartsOn: 1 });
        const buttons = [];

        for (let i = 0; i < 7; i++) {
            const currentDay = addDays(startOfWeekDate, i);
            const isDisabled = currentDay.getDate() < today.getDate();

            buttons.push(
                <Button
                    key={i}
                    variant={schedule === format(currentDay, 'yyyy-MM-dd') ? 'contained' : 'outlined'}
                    color={schedule === format(currentDay, 'yyyy-MM-dd') ? 'primary' : 'inherit'}
                    onClick={() => handleDateChange(currentDay)}
                    disabled={isDisabled}
                    sx={{ mx: 1, my: 1 }}
                >
                    {format(currentDay, 'EEE')}
                </Button>
            );
        }
        return buttons;
    };

    const renderTimeButtons = () => {
        return availableTimes.map((time, index) => (
            <Grid item xs={12} sm={6} md={4} key={index} sx={{ mb: 1 }}>
                <Box
                    sx={{
                        border: '1px solid lightgray',
                        borderRadius: '4px',
                        p: 1,
                        textAlign: 'center',
                        backgroundColor: '#f5f5f5',
                    }}
                    display={"flex"}
                    alignItems={'center'}
                    justifyContent={'center'}
                    gap={1}
                >
                    <Typography variant="body1">{time}</Typography>
                    <IconButton onClick={() => handleDeleteTime(time)}>
                        <CancelIcon color='error' />
                    </IconButton>
                </Box>
            </Grid>
        ));
    };

    const boxStyle = {
        img: {
            maxWidth: '100%',
            height: 'auto',
        },
        wordWrap: 'break-word',
    };

    return (
        <Grid container sx={{ height: 'auto', width: "100%", overflowX: "hidden" }} py={5}>
            <Grid item xs={2} />
            <Grid item xs={8}>
                <Grid container sx={{ height: "auto", width: "100%" }}>
                    <Grid item xs={12} px={2}>

                        <Box sx={{ display: "flex", alignItems: 'center', mb: 5 }}>
                            <Box sx={{ borderRadius: "50%", overflow: 'hidden', maxWidth: "18%", height: "auto" }} border={1}>
                                <img
                                    src="https://fiverr-res.cloudinary.com/image/upload/f_auto,q_auto,t_profile_original/v1/attachments/profile/photo/a5a33e6482d09778e33981e496056e19-1666593838077/71e44813-00fe-4e67-9e7c-b9123754e95a.jpg"
                                    alt='avatartutor'
                                    style={{ width: '100%', height: '100%', objectFit: "cover", objectPosition: "center" }}
                                />
                            </Box>
                            <Box ml={3}>
                                <Typography ml={0.5} variant='h4'>Nguyễn Văn Phú</Typography>
                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 0.5 }} /> <Typography variant='subtitle1' fontWeight={"bold"}>4.8</Typography><Typography variant='body1' ml={1}>(385 lượt đánh giá)</Typography></Stack>
                                <Typography variant='body1' ml={0.5}>Đã tham gia: 12-06-2024</Typography>
                                <Stack direction={"row"} alignItems={"center"} gap={2}>
                                    <Box sx={{ display: 'flex' }}>
                                        <LocationOnOutlinedIcon color='error' sx={{ mr: 0.5 }} />
                                        <Typography variant='subtitle1'>Hồ Chí Minh</Typography>
                                    </Box>
                                    <Box sx={{ display: 'flex' }}>
                                        <RecordVoiceOverOutlinedIcon color='primary' sx={{ mr: 0.5 }} />
                                        <Typography variant='subtitle1'>Tiếng Việt, Tiếng Anh</Typography>
                                    </Box>
                                </Stack>
                            </Box>

                        </Box>

                        <Box sx={{ width: '100%', typography: 'body1' }}>
                            <TabContext value={value}>
                                <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                                    <TabList onChange={handleChange} aria-label="lab API tabs example">
                                        <Tab label="Giới thiệu" value="1" />
                                        <Tab label="Bài tập" value="2" />
                                        <Tab label="Chứng chỉ" value="3" />
                                    </TabList>
                                </Box>
                                <TabPanel value="1">
                                    <>
                                        <Box boxShadow={2} my={5} sx={{ borderRadius: "10px", maxHeight: "500px" }} pb={5}>
                                            <Box bgcolor={'rgb(168 85 247)'} p={2} sx={{ borderBottom: "1px solid", borderColor: "lightgray", borderTopLeftRadius: "10px", borderTopRightRadius: "10px" }}>
                                                <Typography variant='h6' color={'white'} ml={2}>Tổng quan</Typography>
                                            </Box>

                                            <Box pl={2}>
                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }} mt={2}>
                                                    <PaidOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '50px' }}>Học phí: </Typography>
                                                    <Typography variant='h6'>3.000.000/ buổi</Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <CakeOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '150px' }}>Độ tuổi học sinh, học viên: </Typography>
                                                    <Typography variant='h6'>12 tháng - 1 tuổi</Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <LocalPhoneOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '30px' }}>Số điện thoại: </Typography>
                                                    <a href='tel:40404040404'>
                                                        <Typography variant='h6' sx={{
                                                            '&:hover': { color: "blue" }
                                                        }}>035484151</Typography>
                                                    </a>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <EmailOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '50px' }}>Email: </Typography>
                                                    <Typography variant='h6'>nguyenvanphu@gmail.com</Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, p: 1 }}>
                                                    <LocationOnOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '60px' }}>Địa chỉ: </Typography>
                                                    <Typography variant='h6'>204 Nguyễn Lương Bằng, Phường Quang Trung, Quận Đống Đa, TP. Hà Nội</Typography>
                                                </Box>
                                            </Box>
                                        </Box>

                                        <Box display="flex" flexDirection="column" gap={3}>
                                            <Box>
                                                <Stack direction={'row'} gap={1} alignItems={'center'}>
                                                    <Typography my={2} variant='h5'>Giới thiệu về tôi</Typography>
                                                    <EditAboutMe text={about} setText={setAbout} />
                                                </Stack>
                                                <Box sx={boxStyle} dangerouslySetInnerHTML={{ __html: about }} />
                                            </Box>

                                            <Box sx={{ borderTop: "1px solid", borderColor: "lightgray" }}>
                                                <Typography my={2} variant='h5'>Học Vấn</Typography>
                                                <Stack direction={'column'} gap={1}>
                                                    <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }}>
                                                        <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                            {/* <img style={{ width: '100%', height: 'auto', objectFit: "cover", objectPosition: "center", borderRadius: "10px" }}
                                                src="https://fiverr-res.cloudinary.com/npm-assets/@fiverr/seller_page_perseus/Education.9b53994.svg" /> */}
                                                            <SchoolOutlinedIcon fontSize='large' />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='h6' fontSize={'medium'}>Đại học kinh tế quốc dân (NEU)</Typography>
                                                            <Typography variant='body1'>Chuyên ngành: Tâm lý giáo dục</Typography>
                                                            <Typography variant='body2'>Từ 06-2016 đến 08-2020</Typography>
                                                        </Box>
                                                    </Box>
                                                    <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }}>
                                                        <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                            <SchoolOutlinedIcon fontSize='large' />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='h6' fontSize={'medium'}>Đại học sư phạm Hà Nội (HNUE)</Typography>
                                                            <Typography variant='body1'>Chuyên ngành: Tâm lý giáo dục</Typography>
                                                            <Typography variant='body2'>Từ 06-2016 đến 08-2020</Typography>
                                                        </Box>
                                                    </Box>
                                                    <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }}>
                                                        <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                            <SchoolOutlinedIcon fontSize='large' />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='h6' fontSize={'medium'}>Đại học Bách Khoa (HUST)</Typography>
                                                            <Typography variant='body1'>Chuyên ngành: Tâm lý giáo dục</Typography>
                                                            <Typography variant='body2'>Từ 06-2016 đến 08-2020</Typography>
                                                        </Box>
                                                    </Box>
                                                </Stack>
                                            </Box>
                                            <Box py={3} sx={{ borderTop: "1px solid", borderBottom: "1px solid", borderColor: "lightgray" }}>
                                                <Typography mb={2} variant='h5'>Kinh nghiệm làm việc</Typography>
                                                <Stack direction={'column'} gap={1}>
                                                    <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }}>
                                                        <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>

                                                            <BusinessCenterOutlinedIcon fontSize='large' />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='h6' fontSize={'medium'}>Trung tâm giáo dục IPro</Typography>
                                                            <Typography variant='body1'>Vị trí: Giáo viên chủ nhiệm</Typography>
                                                            <Typography variant='body2'>Từ 06-2016 đến 08-2020</Typography>
                                                        </Box>
                                                    </Box>
                                                    <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }}>
                                                        <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                            <BusinessCenterOutlinedIcon fontSize='large' />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='h6' fontSize={'medium'}>Trung tâm Vina Health</Typography>
                                                            <Typography variant='body1'>Vị trí: Chuyên gia tâm lý</Typography>
                                                            <Typography variant='body2'>Từ 06-2016 đến 08-2020</Typography>
                                                        </Box>
                                                    </Box>

                                                </Stack>
                                            </Box>

                                            <Box pb={2} sx={{ borderBottom: "1px solid", borderColor: "lightgray" }}>
                                                <Typography mb={2} variant='h5'>Khung chương trình học</Typography>
                                                <TabContext value={valueCurriculum}>
                                                    <Box sx={{ maxWidth: { xs: 320, sm: 480 } }}>
                                                        <Tabs
                                                            value={valueCurriculum}
                                                            onChange={handleChangeCurriculum}
                                                            // variant="scrollable"
                                                            // scrollButtons
                                                            aria-label="icon position tabs example"
                                                        >
                                                            <Tab value="1" icon={<ElevatorIcon />} iconPosition="start" label="Từ 0 - 3 tuổi" />
                                                            <Tab value="2" icon={<ElevatorIcon />} iconPosition="start" label="Từ 4 - 6 tuổi" />
                                                            <Tab value="3" icon={<ElevatorIcon />} iconPosition="start" label="Từ 7 - 9 tuổi" />
                                                            {/* <Tab value="4" icon={<ElevatorIcon />} iconPosition="start" label="Từ 7 - 9 tuổi" />
                                                            <Tab value="5" icon={<ElevatorIcon />} iconPosition="start" label="Từ 7 - 9 tuổi" />
                                                            <Tab value="6" icon={<ElevatorIcon />} iconPosition="start" label="Từ 7 - 9 tuổi" /> */}
                                                        </Tabs>
                                                    </Box>
                                                    <TabPanel value="1"> {learnGoal.split('\n').map((line, index) => (
                                                        <Typography variant='subtitle1' key={index}>
                                                            {line}
                                                        </Typography>
                                                    ))}</TabPanel>
                                                    <TabPanel value="2"> {additionalContent.split('\n').map((line, index) => (
                                                        <Typography variant='subtitle1' key={index}>
                                                            {line}
                                                        </Typography>
                                                    ))}</TabPanel>
                                                    <TabPanel value="3"> {learnGoal.split('\n').map((line, index) => (
                                                        <Typography variant='subtitle1' key={index}>
                                                            {line}
                                                        </Typography>
                                                    ))}</TabPanel>
                                                </TabContext>

                                            </Box>

                                            <Box bgcolor={'#fff8e3'} p={3} borderRadius={'20px'}>
                                                <Stack direction={'row'} gap={1} alignItems={'center'}>
                                                    <Typography my={2} variant='h5'>Mục tiêu học tập</Typography>
                                                    <EditAboutMe text={learnGoal} setText={setLearnGoal} />
                                                </Stack>
                                                <Stack direction={'row'} gap={2}>
                                                    <Box sx={{ width: "5%" }} pt={2}>
                                                        <CheckCircleIcon color='success' fontSize='large' />
                                                    </Box>
                                                    <Box sx={{
                                                        width: "85%",
                                                        img: {
                                                            maxWidth: '100%',
                                                            height: 'auto',
                                                        },
                                                        wordWrap: 'break-word',
                                                    }} dangerouslySetInnerHTML={{ __html: learnGoal }} />
                                                    <Box sx={{ width: "10%", display: "flex", alignItems: "end" }}>
                                                        <img src='https://cdn-icons-png.freepik.com/256/4295/4295914.png?semt=ais_hybrid'
                                                            style={{ width: "100%", objectFit: "cover", objectPosition: "center" }}
                                                        />
                                                    </Box>
                                                </Stack>
                                            </Box>
                                            <Box sx={{ borderTop: "1px solid", borderColor: "lightgray" }}>
                                                <Typography my={2} variant='h5'>Thời gian có sẵn</Typography>
                                                <Stack direction={'column'} gap={1}>
                                                    <Box sx={{ display: 'flex', flexWrap: 'wrap' }}>
                                                        {renderWeekButtons()}
                                                    </Box>
                                                    <Stack direction={'row'} gap={2} mt={2}>
                                                        <Box>
                                                            <Typography variant='body1' mb={2}>Thời gian bắt đầu</Typography>
                                                            <TextField
                                                                id="outlined-basic1"
                                                                label=""
                                                                variant="outlined"
                                                                type="time"
                                                                value={startTime}
                                                                onChange={(e) => setStartTime(e.target.value)}
                                                            />
                                                        </Box>
                                                        <Box>
                                                            <Typography variant='body1' mb={2}>Thời gian kết thúc</Typography>
                                                            <TextField
                                                                id="outlined-basic2"
                                                                label=""
                                                                variant="outlined"
                                                                type="time"
                                                                value={endTime}
                                                                onChange={(e) => setEndTime(e.target.value)}
                                                            />
                                                        </Box>
                                                    </Stack>
                                                    <Box my={1}>
                                                        <Button color='primary' variant='contained' onClick={handleSave}>
                                                            Lưu
                                                        </Button>
                                                    </Box>
                                                    <Box sx={{ display: 'flex', flexDirection: 'column', flexWrap: 'wrap' }}>
                                                        <Typography variant='h6'>{format(new Date(schedule), 'EEEE')}</Typography>
                                                        <Grid container spacing={1} sx={{ mt: 1 }}>
                                                            {renderTimeButtons()}
                                                        </Grid>
                                                    </Box>
                                                </Stack>
                                            </Box>
                                            <Reviews />
                                        </Box>
                                    </>
                                </TabPanel>
                                <TabPanel value="2">Bài tập</TabPanel>
                                <TabPanel value="3">Chứng chỉ</TabPanel>
                            </TabContext>
                        </Box>

                    </Grid>

                </Grid>
            </Grid>

            <Grid item xs={2} />
        </Grid>
    )
}

export default TutorProfileUpdate;
