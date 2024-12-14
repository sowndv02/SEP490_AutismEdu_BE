import BusinessCenterOutlinedIcon from '@mui/icons-material/BusinessCenterOutlined';
import CakeOutlinedIcon from '@mui/icons-material/CakeOutlined';
import CelebrationIcon from '@mui/icons-material/Celebration';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ElevatorIcon from '@mui/icons-material/Elevator';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import PaidOutlinedIcon from '@mui/icons-material/PaidOutlined';
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined';
import LockIcon from '@mui/icons-material/Lock';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import StarIcon from '@mui/icons-material/Star';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import { Avatar, Box, Button, Divider, Grid, Paper, Skeleton, Stack, Typography } from '@mui/material';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import { format } from 'date-fns';
import { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import TutorRating from './TutorRating';

function TutorProfile() {
    const { id } = useParams();

    const introductionRef = useRef(null);
    const educationRef = useRef(null);
    const experienceRef = useRef(null);
    const curriculumRef = useRef(null);
    const availableTimeRef = useRef(null);
    const reviewRef = useRef(null);
    const [timeData, setTimeData] = useState(1);
    const [availability, setAvailability] = useState([]);
    const [value, setValue] = useState('1');
    const [valueCurriculum, setValueCurriculum] = useState('1');
    const [tutor, setTutor] = useState(null);
    const [loading, setLoading] = useState(false);
    console.log(tutor);

    useEffect(() => {
        handleGetAllAvailableTime(1);
    }, [id]);


    const handleDateChange = async (weekday) => {
        setTimeData(weekday);
        await handleGetAllAvailableTime(weekday);
    };

    const handleGetAllAvailableTime = async (weekday) => {
        try {
            await services.AvailableTimeManagementAPI.getAvailableTime((res) => {
                setAvailability(res.result || []);
            }, (error) => {
                console.log(error);
            }, { tutorId: id, weekday });
        } catch (error) {
            console.log(error);
        }
    };

    const handleChange = (event, newValue) => {
        switch (newValue) {
            case "1":
                setValue(newValue);
                introductionRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            case "2":
                setValue(newValue);
                educationRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            case "3":
                setValue(newValue);
                experienceRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            case "4":
                setValue(newValue);
                curriculumRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            case "5":
                setValue(newValue);
                availableTimeRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            case "6":
                setValue(newValue);
                reviewRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
                break;
            default:
                break;
        }
    };
    const handleChangeCurriculum = (event, newValue) => {
        setValueCurriculum(newValue);
    };

    useEffect(() => {
        window.scrollTo(0, 0);
        handleGetProfile(id);
    }, [id]);

    const handleGetProfile = async (id) => {
        setLoading(true);
        try {
            await services.UserManagementAPI.getUserById(id, (res) => {
                setTutor(res.result);
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (dateString) => {
        return format(new Date(dateString), 'dd/MM/yyyy');
    };

    const formatDateCer = (dateString) => {
        return format(new Date(dateString), 'dd-MM-yyyy');
    };

    const getCity = (address) => {
        const city = address.split('|')[0];
        return city;
    };

    const calculateAge = (birthDate) => {

        const today = new Date();
        let age = today.getFullYear() - birthDate.getFullYear();
        const monthDifference = today.getMonth() - birthDate.getMonth();

        if (monthDifference < 0 || (monthDifference === 0 && today.getDate() < birthDate.getDate())) {
            age--;
        }

        return age;
    };

    const formatter = new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
    });

    const formatAddress = (address) => {
        let adrs = address.split('|');
        return adrs.reverse().join(', ');
    };

    const renderWeekButtons = () => {
        const weekDays = [
            { date: 1, label: 'Thứ 2' },
            { date: 2, label: 'Thứ 3' },
            { date: 3, label: 'Thứ 4' },
            { date: 4, label: 'Thứ 5' },
            { date: 5, label: 'Thứ 6' },
            { date: 6, label: 'Thứ 7' },
            { date: 7, label: 'Chủ Nhật' }
        ];

        return weekDays.map((day) => (
            <Button
                key={day.date}
                variant={timeData === day.date ? 'contained' : 'outlined'}
                color={timeData === day.date ? 'primary' : 'inherit'}
                onClick={() => handleDateChange(day.date)}
                sx={{ mr: 2, my: 1 }}
            >
                {day.label}
            </Button>
        ));
    };

    const handleLock = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.lockUsers(id, (res) => {
                if (res?.result) {
                    tutor.isLockedOut = res.result?.isLockedOut;
                    setTutor(tutor);
                    enqueueSnackbar("Khoá tài khoản thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);

            })
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const handleUnlock = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.unLockUsers(id, (res) => {
                if (res?.result) {
                    tutor.isLockedOut = res.result?.isLockedOut;
                    setTutor(tutor);
                    enqueueSnackbar("Mở khoá tài khoản thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);

            })
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const renderTimeButtons = () => {
        if (availability.length === 0) return <Grid item xs={12} sm={6} md={4} sx={{ mb: 1 }}><Typography variant='inherit'>Hiện chưa có khung thời gian</Typography></Grid>
        return availability.map((time, index) => (
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
                    <Typography variant="body1">{time.timeSlot}</Typography>
                </Box>
            </Grid>
        ));
    };

    return (
        <Grid container sx={{ height: 'auto', width: "100%" }} >
            <Grid item xs={12}>
                <Grid component={Paper} container sx={{ height: "auto", width: "100%" }}>
                    {tutor ?
                        <Grid item xs={12} px={4}>

                            <Typography variant='h4' textAlign={'center'} my={2}>Hồ sơ gia sư</Typography>
                            <Divider />
                            <Box sx={{ display: "flex", alignItems: 'center', my: 5, width: "100%" }}>
                                <Box sx={{ overflow: 'hidden', width: "25%", height: "auto" }} display={'flex'} justifyContent={'center'}>
                                    <Avatar
                                        src={tutor?.imageUrl}
                                        alt='avatartutor'
                                        style={{ width: '200px', height: '200px' }}
                                    />
                                </Box>
                                <Box ml={1} sx={{ width: "55%" }}>
                                    <Typography ml={0.5} variant='h4'>{tutor?.fullName}</Typography>
                                    <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 0.5 }} /> <Typography variant='subtitle1' fontWeight={"bold"}>{tutor?.tutorProfile?.reviewScore}</Typography><Typography variant='body1' ml={1}>({tutor?.tutorProfile?.totalReview} lượt đánh giá)</Typography></Stack>
                                    <Typography variant='body1' ml={0.5}>Đã tham gia: {tutor?.tutorProfile?.createdDate && formatDate(tutor?.tutorProfile?.createdDate)}</Typography>
                                    <Stack direction={"row"} alignItems={"center"} gap={2}>
                                        <Box sx={{ display: 'flex' }}>
                                            <LocationOnOutlinedIcon color='error' sx={{ mr: 0.5 }} />
                                            <Typography variant='subtitle1'>{tutor?.address ? getCity(tutor?.address) : 'Hồ Chí Minh'}</Typography>
                                        </Box>
                                        <Box sx={{ display: 'flex' }}>
                                            <CelebrationIcon color='info' sx={{ mr: 0.5 }} />
                                            <Typography variant='subtitle1'>{tutor?.tutorProfile?.dateOfBirth && calculateAge(new Date(tutor?.tutorProfile?.dateOfBirth))} tuổi</Typography>
                                        </Box>
                                    </Stack>
                                </Box>
                                <Box sx={{ width: "20%" }}>
                                    {!tutor.isLockedOut ? <Button
                                        startIcon={<LockIcon />}
                                        variant="contained"
                                        color="error"
                                        onClick={() => handleLock()}
                                        sx={{ fontSize: '13px' }}
                                    >
                                        Khoá tài khoản
                                    </Button>
                                        :
                                        <Button
                                            startIcon={<LockOpenIcon />}
                                            variant="contained"
                                            color="success"
                                            onClick={() => handleUnlock()}
                                        >
                                            Mở khoá
                                        </Button>}
                                </Box>
                            </Box>

                            <Box sx={{ width: '100%', typography: 'body1' }}>
                                <TabContext value={value}>
                                    <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                                        <TabList onChange={handleChange} aria-label="lab API tabs example">
                                            <Tab label="Giới thiệu" value="1" />
                                            <Tab label="Học vấn" value="2" />
                                            <Tab label="Kinh nghiệm làm việc" value="3" />
                                            <Tab label="Khung chương trình học" value="4" />
                                            <Tab label="Thời gian rảnh" value="5" />
                                            <Tab label="Đánh giá" value="6" />
                                        </TabList>
                                    </Box>
                                    <>
                                        <Box boxShadow={2} my={5} sx={{ borderRadius: "10px", maxHeight: "500px" }} pb={5}>
                                            <Box bgcolor={'rgb(168 85 247)'} p={2} sx={{ borderBottom: "1px solid", borderColor: "lightgray", borderTopLeftRadius: "10px", borderTopRightRadius: "10px" }}>
                                                <Typography variant='h6' color={'white'} ml={2}>Tổng quan</Typography>
                                            </Box>

                                            <Box pl={2}>
                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }} mt={2}>
                                                    <PaidOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '50px' }}>Học phí: </Typography>
                                                    <Typography variant='h6'>{formatter.format(tutor?.tutorProfile?.priceFrom) + ' - ' + formatter.format(tutor?.tutorProfile?.priceEnd)}<Typography component="span" variant='body1'> / buổi <small>({tutor?.sessionHours} tiếng)</small></Typography></Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <CakeOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '150px' }}>Độ tuổi học sinh, học viên: </Typography>
                                                    <Typography variant='h6'>Từ {tutor?.tutorProfile?.startAge} - {tutor?.tutorProfile?.endAge} tuổi</Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <LocalPhoneOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '30px' }}>Số điện thoại: </Typography>
                                                    <a href={`tel:${tutor.phoneNumber}`}>
                                                        <Typography variant='h6' sx={{
                                                            '&:hover': { color: "blue" }
                                                        }}>{tutor.phoneNumber}</Typography>
                                                    </a>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1 }}>
                                                    <EmailOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '50px' }}>Email: </Typography>
                                                    <Typography variant='h6'>{tutor?.email}</Typography>
                                                </Box>

                                                <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, p: 1 }}>
                                                    <LocationOnOutlinedIcon />
                                                    <Typography variant='subtitle1' sx={{ minWidth: '60px' }}>Địa chỉ: </Typography>
                                                    <Typography variant='h6'>{tutor?.address && formatAddress(tutor?.address)}</Typography>
                                                </Box>
                                            </Box>
                                        </Box>

                                        <Box display="flex" flexDirection="column" gap={3}>
                                            <Box ref={introductionRef}>
                                                <Typography my={2} variant='h5'>Giới thiệu về tôi</Typography>
                                                <Box width={'100%'} bgcolor={'#fff8e3'} mt={2} p={3} borderRadius={'20px'} dangerouslySetInnerHTML={{ __html: tutor?.tutorProfile?.aboutMe }} />

                                            </Box>

                                            <Box sx={{ borderTop: "1px solid", borderColor: "lightgray" }} ref={educationRef}>
                                                <Typography my={2} variant='h5' mb={3}>Chứng chỉ</Typography>
                                                <Stack direction={'column'} gap={2}>
                                                    {tutor?.tutorProfile?.certificates?.length === 0 ? <Typography variant='inherit'>Chưa có dữ liệu chứng chỉ</Typography> : tutor?.tutorProfile?.certificates?.map((cer, index) => (
                                                        <Box sx={{ width: "100%", display: 'flex', direction: 'row', gap: 2 }} key={cer.id}>
                                                            <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                                <SchoolOutlinedIcon fontSize='large' />
                                                            </Box>
                                                            <Box width={'80%'}>
                                                                <Typography variant='h6' fontSize={'medium'}>{cer.certificateName}</Typography>
                                                                <Typography variant='body1'>Cơ quan phát hành: {cer.issuingInstitution}</Typography>
                                                                <Typography variant='body1'>Từ ngày {formatDateCer(cer.issuingDate)} {cer?.expirationDate && `đến ${formatDateCer(cer?.expirationDate)}`}</Typography>
                                                            </Box>
                                                            <Box width={'10%'}>
                                                                <Button variant='outlined' color={cer.requestStatus === 1 ? 'success' : cer.requestStatus === 0 ? 'error' : 'warning'}>
                                                                    {cer.requestStatus === 1 ? 'Chấp nhận' : cer.requestStatus === 0 ? 'Từ chối' : 'Chờ duyệt'}
                                                                </Button>
                                                            </Box>
                                                        </Box>
                                                    ))
                                                    }

                                                </Stack>
                                            </Box>
                                            <Box py={3} sx={{ borderTop: "1px solid", borderBottom: "1px solid", borderColor: "lightgray" }} ref={experienceRef}>
                                                <Typography mb={2} variant='h5'>Kinh nghiệm làm việc</Typography>
                                                <Stack direction={'column'} gap={1}>
                                                    {tutor?.tutorProfile?.workExperiences?.length === 0 ? (
                                                        <Typography variant='inherit'>Chưa có dữ liệu về kinh nghiệm làm việc</Typography>
                                                    ) : (
                                                        tutor?.tutorProfile?.workExperiences?.map((work, index) => (
                                                            <Box key={index} sx={{ width: "100%", display: 'flex', flexDirection: 'row', gap: 2 }}>
                                                                <Box sx={{ maxWidth: "10%", height: "auto", borderRadius: "10px" }}>
                                                                    <BusinessCenterOutlinedIcon fontSize='large' />
                                                                </Box>
                                                                <Box width={'80%'}>
                                                                    <Typography variant='h6' fontSize={'medium'}>{work.companyName}</Typography>
                                                                    <Typography variant='body1'>Vị trí: {work.position}</Typography>
                                                                    <Typography variant='body2'>Từ ngày {formatDateCer(work.startDate)} {work?.endDate && `đến ${formatDateCer(work?.endDate)}`}</Typography>
                                                                </Box>
                                                                <Box width={'10%'}>
                                                                    <Button variant='outlined' color={work.requestStatus === 1 ? 'success' : work.requestStatus === 0 ? 'error' : 'warning'}>
                                                                        {work.requestStatus === 1 ? 'Chấp nhận' : work.requestStatus === 0 ? 'Từ chối' : 'Chờ duyệt'}
                                                                    </Button>
                                                                </Box>
                                                            </Box>
                                                        ))
                                                    )}


                                                </Stack>
                                            </Box>

                                            <Box pb={2} ref={curriculumRef}>
                                                <Typography mb={2} variant='h5'>Khung chương trình học</Typography>
                                                {tutor?.tutorProfile?.curriculums?.length === 0 ? <Typography variant='inherit'>Chưa có dữ liệu khung chương trình học</Typography> :
                                                    <TabContext value={valueCurriculum}>
                                                        <Box sx={{ maxWidth: { xs: 320, sm: 480 } }}>
                                                            <Tabs
                                                                value={valueCurriculum}
                                                                onChange={handleChangeCurriculum}
                                                                variant="scrollable"
                                                                scrollButtons="auto"
                                                                aria-label="icon position tabs example"
                                                            >
                                                                {tutor?.tutorProfile?.curriculums?.map((cur, index) => (
                                                                    <Tab value={`${index + 1}`} icon={<ElevatorIcon />} iconPosition="start" label={`Từ ${cur.ageFrom} - ${cur.ageEnd} tuổi`} key={cur.id} />
                                                                ))}

                                                            </Tabs>
                                                        </Box>
                                                        {tutor?.tutorProfile?.curriculums?.map((cur, index) => (
                                                            <TabPanel value={`${index + 1}`} sx={{ padding: '0' }} key={cur.id}>
                                                                <Box display={'flex'} justifyContent={'flex-end'} mt={2}>
                                                                    <Button variant='outlined' color={cur.requestStatus === 1 ? 'success' : cur.requestStatus === 0 ? 'error' : 'warning'}>
                                                                        {cur.requestStatus === 1 ? 'Chấp nhận' : cur.requestStatus === 0 ? 'Từ chối' : 'Chờ duyệt'}
                                                                    </Button>
                                                                </Box>
                                                                <Stack direction={'row'} gap={2} bgcolor={'#fff8e3'} mt={2} p={3} borderRadius={'20px'}>
                                                                    <Box sx={{ width: "5%" }}>
                                                                        <CheckCircleIcon color='success' fontSize='large' />
                                                                    </Box>
                                                                    <Box sx={{ width: "85%" }} dangerouslySetInnerHTML={{ __html: cur.description }} />
                                                                    <Box sx={{ width: "10%", display: "flex", alignItems: "end" }}>
                                                                        <img src='https://cdn-icons-png.freepik.com/256/4295/4295914.png?semt=ais_hybrid'
                                                                            style={{ width: "100%", objectFit: "cover", objectPosition: "center" }}
                                                                        />
                                                                    </Box>
                                                                </Stack>
                                                            </TabPanel>
                                                        ))}

                                                    </TabContext>
                                                }

                                            </Box>
                                            <Divider />


                                            <Stack ref={availableTimeRef} direction='column' sx={{
                                                width: "100%",
                                                margin: "auto",
                                                mt: "20px",
                                                gap: 2
                                            }}>
                                                <Typography variant='h5' my={2}>Thời gian rảnh</Typography>
                                                <Box>
                                                    <Stack direction={'column'} gap={1}>
                                                        <Box sx={{ display: 'flex', flexWrap: 'wrap' }}>
                                                            {renderWeekButtons()}
                                                        </Box>

                                                        <Box sx={{ display: 'flex', flexDirection: 'column', flexWrap: 'wrap' }}>
                                                            <Typography variant='h6'>{`Thứ ${timeData + 1}`}</Typography>
                                                            <Grid container spacing={1} sx={{ mt: 1 }}>
                                                                {renderTimeButtons()}
                                                            </Grid>
                                                        </Box>
                                                    </Stack>
                                                </Box>
                                            </Stack>
                                            <Divider ref={reviewRef} />
                                            {id && <TutorRating tutorId={id} />}
                                            <Divider />
                                        </Box>
                                    </>
                                    {/* <TabPanel value="2">Bài tập</TabPanel>
<TabPanel value="3">Chứng chỉ</TabPanel> */}
                                </TabContext>
                            </Box>

                        </Grid> : <Grid item xs={12} px={2}>
                            <Box sx={{ display: "flex", alignItems: 'center', mb: 5, width: "100%" }}>
                                <Skeleton variant="circular" width={200} height={200} />
                                <Box ml={1} sx={{ width: "55%" }}>
                                    <Skeleton variant="text" width="60%" height={40} />
                                    <Skeleton variant="text" width="40%" height={20} />
                                    <Skeleton variant="text" width="30%" height={20} />
                                    <Skeleton variant="text" width="70%" height={20} />
                                </Box>
                                <Box sx={{ width: "20%" }}>
                                    <Skeleton variant="rectangular" width={120} height={40} />
                                </Box>
                            </Box>

                            <Box sx={{ width: '100%', typography: 'body1' }}>
                                <Skeleton variant="text" width="20%" height={30} />
                                <Skeleton variant="rectangular" width="100%" height={500} sx={{ my: 2, borderRadius: "10px" }} />
                            </Box>

                            <Skeleton variant="text" width="30%" height={30} />
                            <Skeleton variant="rectangular" width="100%" height={200} sx={{ my: 2, borderRadius: "10px" }} />

                            <Skeleton variant="text" width="30%" height={30} />
                            <Skeleton variant="rectangular" width="100%" height={150} sx={{ my: 2, borderRadius: "10px" }} />
                        </Grid>}

                </Grid>
                <LoadingComponent open={loading} setOpen={setLoading} />
            </Grid>
        </Grid >
    )
}

export default TutorProfile;
