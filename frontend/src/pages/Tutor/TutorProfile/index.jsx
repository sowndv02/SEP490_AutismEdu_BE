import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, Stack, Typography } from '@mui/material'
import StarIcon from '@mui/icons-material/Star';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined';
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import BusinessCenterOutlinedIcon from '@mui/icons-material/BusinessCenterOutlined';
import PaidOutlinedIcon from '@mui/icons-material/PaidOutlined';
import CakeOutlinedIcon from '@mui/icons-material/CakeOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import React, { useState } from 'react'
import { format, addDays, startOfWeek } from 'date-fns';

function TutorProfile() {
    const today = new Date();
    const [learnGoal, setLearnGoal] = useState("Con bạn sẽ:\n* Học nhiều chiến lược đọc và cách áp dụng chúng.\n* Hiểu câu chuyện qua các câu hỏi hiểu bài.\n* Luyện tập từ vựng nhìn thấy\n* Luyện tập độ trôi chảy, từ vựng và nhận thức âm thanh.\n* Luyện tập lượt chơi, kỹ năng lắng nghe, kiên nhẫn và nhận thức xã hội.\n* Luyện tập cấu trúc câu qua viết mô phỏng.");
    const [schedule, setSchedule] = useState(format(today, 'yyyy-MM-dd'));

    const [availableTimes, setAvailableTimes] = useState([]);
    const handleDateChange = (date) => {
        setSchedule(format(date, 'yyyy-MM-dd'));
        const times = [
            '8:30 – 9:00',
            '13:00 – 13:30',
            '15:00 – 15:30',
            '16:00 – 16:30',
        ];
        setAvailableTimes(times);
    };


    const renderWeekButtons = () => {
        const today = new Date();
        const startOfWeekDate = startOfWeek(today, { weekStartsOn: 1 }); // Week starts on Monday
        const buttons = [];

        for (let i = 0; i < 7; i++) {
            const currentDay = addDays(startOfWeekDate, i);
            const dayCurrent = currentDay.getDate();
            const dayToday = today.getDate();
            const isDisabled = dayCurrent < dayToday;

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
                >
                    <Typography variant="body1">
                        {time}
                    </Typography>
                </Box>
            </Grid>
        ));
    };
    return (
        <Grid container sx={{ height: 'auto', width: "100%" }} py={5}>
            <Grid item xs={2} />
            <Grid item xs={8}>
                <Grid container sx={{ height: "auto", width: "100%" }}>
                    <Grid item xs={8} px={2}>
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
                        <Box display="flex" flexDirection="column" gap={3}>
                            <Box>
                                <Typography my={2} variant='h5'>Giới thiệu về tôi</Typography>
                                <Typography variant='body1'>
                                    Tên tôi là John Rotgers và tôi tốt nghiệp Thạc sĩ Quản trị Kinh doanh chuyên ngành Tiếp thị (Trực tuyến) tại Đại học Erasmus Rotterdam. Tôi là một chuyên gia chuyên về thương mại điện tử từ năm 2001. Tôi đã làm việc cho một số công ty quốc tế, như TNT Post và Centric. Ngoài ra, tôi còn làm tư vấn với tư cách là một nhà quản lý kinh doanh điện tử tự do. Tôi cũng đã thiết lập nhiều trang web và cửa hàng trực tuyến thành công trong lĩnh vực bán lẻ và du lịch. Tôi có thể giúp bạn với nhiều chủ đề liên quan đến kinh doanh điện tử và tiếp thị trực tuyến như viết văn bản, SEO, Google Ads và phân tích từ khóa và trang web.
                                </Typography>
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
                            <Box sx={{ borderTop: "1px solid", borderColor: "lightgray" }}>
                                <Typography my={2} variant='h5'>Kinh nghiệm làm việc</Typography>
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
                            <Box bgcolor={'#fff8e3'} p={3} borderRadius={'20px'}>
                                <Typography my={2} variant='h5'>Mục tiêu học tập</Typography>
                                <Stack direction={'row'} gap={2}>
                                    <Box sx={{ width: "5%" }}>
                                        <CheckCircleIcon color='success' fontSize='large' />
                                    </Box>
                                    <Box sx={{ width: "85%" }}>
                                        {learnGoal.split('\n').map((line, index) => (
                                            <Typography variant='subtitle1' key={index}>
                                                {line}
                                                {/* <br /> */}
                                            </Typography>
                                        ))}
                                    </Box>
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
                                    <Box sx={{ display: 'flex', flexDirection: 'column', flexWrap: 'wrap' }}>
                                        <Typography variant='h6'>{format(new Date(schedule), 'EEEE')}</Typography>
                                        <Grid container spacing={1} sx={{ mt: 1 }}>
                                            {renderTimeButtons()}
                                        </Grid>
                                    </Box>
                                </Stack>
                            </Box>
                            <Box >
                                <Typography variant='h5' my={2}>Đánh giá</Typography>
                                <Box p={2} boxShadow={2} borderRadius={2}>
                                    <Stack direction={"row"} gap={2} py={1} sx={{ borderBottom: "1px solid", borderColor: "lightgray" }}>
                                        <Box sx={{ width: '8%', borderRadius: "50%" }}>
                                            <img src='https://fiverr-res.cloudinary.com/image/upload/f_auto,q_auto,t_profile_original/v1/attachments/profile/photo/a5a33e6482d09778e33981e496056e19-1666593838077/71e44813-00fe-4e67-9e7c-b9123754e95a.jpg'
                                                alt='avatarcmt' style={{ width: '100%', borderRadius: "50%", objectFit: 'cover', objectPosition: 'center' }}
                                            />
                                        </Box>
                                        <Box sx={{ width: '30%' }}>
                                            <Typography variant='h6'>Lê Quang Hiếu</Typography>
                                        </Box>
                                    </Stack>
                                </Box>
                            </Box>
                        </Box>
                    </Grid>
                    <Grid item xs={4} boxShadow={2} sx={{ borderRadius: "20px", maxHeight: "500px" }}>
                        <Box bgcolor={'rgb(168 85 247)'} p={2} sx={{ borderBottom: "1px solid", borderColor: "lightgray", borderTopLeftRadius: "20px", borderTopRightRadius: "20px" }}>
                            <Typography variant='h6' color={'white'}>Tổng quan</Typography>
                        </Box>

                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
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
                    </Grid>

                </Grid>
            </Grid>
            <Grid item xs={2} />
        </Grid >
    )
}

export default TutorProfile;
