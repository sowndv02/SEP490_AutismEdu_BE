import { Box, Button, FormControl, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { useParams } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { listStudent } from '~/redux/features/listStudent';
import ViewDetailModal from './ScheduleModal/ViewDetailModal';
function StudentSchedule({ studentProfile }) {
    const { id } = useParams();
    const [weekInYears, setWeekInYears] = useState([]);
    const [listYears, setListYears] = useState([]);
    const [currentYear, setCurrentYear] = useState(new Date().getFullYear());
    const [currentWeek, setCurrentWeek] = useState(0);
    const [filterSchedule, setFilterSchedule] = useState(null);
    const [loading, setLoading] = useState(false);
    const [currentStudent, setCurrentStudent] = useState(0);
    const listStudents = useSelector(listStudent);
    const [aSchedule, setASchedule] = useState(null);
    const [isDetailModalOpen, setDetailModalOpen] = useState(false);

    useEffect(() => {
        if (weekInYears.length !== 0 && studentProfile) {
            getSchedule();
        }
    }, [weekInYears, studentProfile])

    const getSchedule = async () => {
        try {
            setLoading(true);
            await services.ScheduleAPI.getSchedule((res) => {
                organizeSchedulesByDay(res.result.schedules)
            }, (err) => {
                console.log(err);
            }, {
                studentProfileId: id,
                startDate: formatDate(weekInYears[currentWeek].monday),
                endDate: formatDate(weekInYears[currentWeek].sunday)
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }

    useEffect(() => {
        if (studentProfile) {
            const year = studentProfile.status === 0 ? new Date(studentProfile.updatedDate).getFullYear() : new Date().getFullYear();
            const weeks = generateMondaysAndSundays(year);
            setWeekInYears(weeks);
            const updatedDate = studentProfile.status === 0 ? resetTime(new Date(studentProfile.updatedDate)) : resetTime(new Date());
            setCurrentWeek(weeks.findIndex(week => updatedDate >= resetTime(week.monday) && updatedDate <= resetTime(week.sunday)));
            const startYear = new Date(studentProfile.createdDate).getFullYear();
            const currentYear = new Date().getFullYear();
            const years = [];
            for (let y = startYear; y <= currentYear; y++) {
                years.push(y);
            }
            years.reverse();
            setListYears(years);
        }
    }, [studentProfile])

    useEffect(() => {
        if (weekInYears.length !== 0) {
            getSchedule();
        }
    }, [weekInYears, currentWeek])

    function generateMondaysAndSundays(year) {
        const result = [];
        const updatedDate = new Date(studentProfile?.updatedDate);
        updatedDate.setHours(0, 0, 0, 0);
        const createdDate = new Date(studentProfile?.createdDate);
        createdDate.setHours(0, 0, 0, 0);
        let date = new Date(year, 0, 1);
        while (date.getDay() !== 0) {
            date.setDate(date.getDate() + 1);
        }
        let monday = new Date(date);
        monday.setDate(monday.getDate() - 6);
        monday.setHours(0, 0, 0, 0)
        while (monday.getFullYear() === year || (monday.getFullYear() < year && monday.getMonth() === 11)) {
            const sunday = new Date(monday);
            sunday.setDate(monday.getDate() + 6);
            sunday.setHours(0, 0, 0, 0);
            result.push({
                monday: new Date(monday), sunday: new Date(sunday),
                mondayText: `${String(monday.getDate()).padStart(2, '0')}/${String(monday.getMonth() + 1).padStart(2, '0')}`,
                sundayText: `${String(sunday.getDate()).padStart(2, '0')}/${String(sunday.getMonth() + 1).padStart(2, '0')}`,
            });
            monday.setDate(monday.getDate() + 7);
        }

        let learntWeeks;
        if (studentProfile.status !== 1) {
            learntWeeks = result.filter((r) => {
                return r.sunday.getTime() >= createdDate.getTime() && r.monday.getTime() <= updatedDate.getTime();
            })
        } else {
            learntWeeks = result.filter((r) => {
                return r.sunday.getTime() >= createdDate.getTime();
            })
        }
        return learntWeeks;
    }

    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');

        const formattedDate = `${year}-${month}-${day}`;
        return formattedDate
    }

    function organizeSchedulesByDay(listSchedule) {
        const days = {
            monday: [],
            tuesday: [],
            wednesday: [],
            thursday: [],
            friday: [],
            saturday: [],
            sunday: [],
        };

        listSchedule.forEach(schedule => {
            const day = new Date(schedule.scheduleDate).getDay();
            switch (day) {
                case 1:
                    days.monday.push(schedule);
                    break;
                case 2:
                    days.tuesday.push(schedule);
                    break;
                case 3:
                    days.wednesday.push(schedule);
                    break;
                case 4:
                    days.thursday.push(schedule);
                    break;
                case 5:
                    days.friday.push(schedule);
                    break;
                case 6:
                    days.saturday.push(schedule);
                    break;
                case 0:
                    days.sunday.push(schedule);
                    break;
                default:
                    break;
            }
        });
        setFilterSchedule(days)
    }

    const formatTime = (timeString) => {
        if (!timeString) {
            return ""
        }
        const [hours, minutes] = timeString.split(':');
        const formattedTime = `${hours}:${minutes}`;
        return formattedTime
    }
    function resetTime(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    const passStatus = (value) => {
        return value === 2 ? 'Chưa có' : value === 1 ? "Đạt" : "Chưa đạt"
    };
    const attendanceStatus = (value) => {
        return value === 2 ? 'Chưa có mặt' : value === 1 ? "Có mặt" : "Vắng"
    };

    const handleViewDetail = (f, keys) => {
        setASchedule(f);
        setDetailModalOpen(true);
    };

    return (
        <>
            <Box p="30px" sx={{ width: "80%", margin: "auto" }}>
                <Stack direction='row' alignItems="center" gap={3}>
                    {
                        !id && (
                            <FormControl sx={{ mb: 1, width: 300 }}>
                                <Select value={currentStudent}
                                    onChange={(e) => setCurrentStudent(e.target.value)}
                                >
                                    <MenuItem value={0}>Tất cả học sinh</MenuItem>
                                    {
                                        listStudents && listStudents.length !== 0 && listStudents.map((s) => {
                                            return (
                                                <MenuItem value={s.id} key={s.id}>{s.name} - {s.studentCode}</MenuItem>
                                            )
                                        })
                                    }
                                </Select>
                            </FormControl>
                        )
                    }

                    <FormControl sx={{ mb: 1, width: 100 }}>
                        <InputLabel id="year">Năm</InputLabel>
                        <Select
                            labelId="year"
                            value={currentYear}
                            label="Năm"
                            onChange={(e) => {
                                setCurrentYear(e.target.value)
                                const weeks = generateMondaysAndSundays(e.target.value);
                                const today = new Date();
                                const index = weeks.findIndex(week => today >= week.monday && today <= week.sunday);
                                setWeekInYears(weeks)
                                setCurrentWeek(index === -1 ? 0 : index);
                            }}
                        >
                            {
                                listYears.map((l) => {
                                    return (
                                        <MenuItem key={l} value={l}>{l}</MenuItem>
                                    )
                                })
                            }
                        </Select>
                    </FormControl>
                    <FormControl sx={{ mb: 1, width: 180 }}>
                        <InputLabel id="week">Tuần</InputLabel>
                        <Select
                            labelId="week"
                            value={currentWeek}
                            label="Tuần"
                            onChange={(e) => setCurrentWeek(e.target.value)}
                        >
                            {
                                weekInYears.map((w, index) => {
                                    return (
                                        <MenuItem key={index} value={index}>{w.mondayText} - {w.sundayText}</MenuItem>
                                    )
                                })
                            }
                        </Select>
                    </FormControl>
                </Stack>
                <Stack direction="row" sx={{ width: "100%", minHeight: "90%" }}>
                    {currentWeek >= 0 && weekInYears[currentWeek] && (
                        <>
                            {Array.from({ length: 7 }, (_, i) => {
                                const day = new Date(weekInYears[currentWeek].monday);
                                day.setDate(day.getDate() + i);
                                const today = new Date();
                                const dayName = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật'][i];
                                const keys = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"][i];
                                return (
                                    <Box key={i} sx={{
                                        width: "14%", minHeight: "80%",
                                        borderTopLeftRadius: i === 0 ? "10px" : "0px",
                                        borderBottomLeftRadius: i === 0 ? "10px" : "0px",
                                        border: "2px solid #d8d8d8",
                                        borderLeft: i !== 0 && "none",
                                        pt: 2,
                                        px: 1,
                                        borderTopRightRadius: i === 6 ? "10px" : "0px",
                                        borderBottomRightRadius: i === 6 ? "10px" : "0px"
                                    }}>
                                        <Typography sx={{ fontSize: "16px", textAlign: "center" }}>{dayName}</Typography>
                                        <Box sx={{
                                            width: "40px", height: "40px", margin: "auto",
                                            borderRadius: "50%",
                                            backgroundColor: today.getDate() === day.getDate() && today.getMonth() === day.getMonth() && "#556cd6",
                                            color: today.getDate() === day.getDate() && today.getMonth() === day.getMonth() && "white"
                                        }}>
                                            <Typography sx={{ fontSize: "22px", textAlign: "center", lineHeight: "40px" }}>{day.getDate()}</Typography>
                                        </Box>
                                        {
                                            filterSchedule && filterSchedule[keys].length !== 0 && filterSchedule[keys].map((f, index) => {
                                                return (
                                                    <Box key={f.id} sx={{
                                                        height: "auto", width: "100%", bgcolor: "#eee9ff", p: 2,
                                                        mb: 1, borderRadius: '10px',
                                                        mt: 2
                                                    }}>
                                                        <Typography sx={{ color: "#7850d4" }}>Mã: {f.studentProfile?.studentCode}</Typography>
                                                        <Typography sx={{ color: "#7850d4", fontWeight: "bold" }}>({formatTime(f.start)} - {formatTime(f.end)})</Typography>
                                                        <Typography my={1} sx={{ color: "black", fontSize: "10px" }}>
                                                            Đánh giá:
                                                            <span style={{
                                                                backgroundColor: f.passingStatus === 1 ? 'green' : f.passingStatus === 2 ? 'orange' : '#f55151',
                                                                color: 'white',
                                                                marginLeft: '4px',
                                                                padding: '2px 4px',
                                                                borderRadius: '4px'
                                                            }}>
                                                                {passStatus(f.passingStatus)}
                                                            </span>
                                                        </Typography>
                                                        {
                                                            f.isUpdatedSchedule === true && (
                                                                <Typography sx={{ color: "green", fontSize: "12px" }}>Lịch đã thay đổi</Typography>
                                                            )
                                                        }
                                                        <Typography sx={{ color: f.attendanceStatus === 1 ? "green" : "red", fontSize: "12px", fontWeight: '500' }} >({attendanceStatus(f.attendanceStatus)})</Typography>
                                                        <Stack direction={'column'} justifyContent={'center'}>
                                                            <Box>
                                                                <Button size='small' variant='contained'
                                                                    color='primary'
                                                                    sx={{ mt: 2, fontSize: "12px" }}
                                                                    onClick={() => handleViewDetail(f, keys)}
                                                                >
                                                                    Xem chi tiết
                                                                </Button>
                                                            </Box>
                                                        </Stack>
                                                    </Box>
                                                )
                                            })
                                        }
                                    </Box>
                                );
                            })}
                        </>
                    )}

                </Stack>
                {aSchedule && studentProfile?.tutor?.fullName && <ViewDetailModal isOpen={isDetailModalOpen} setModalOpen={setDetailModalOpen} schedule={aSchedule} setSchedule={setASchedule} tutorName={studentProfile?.tutor?.fullName} />}
                <LoadingComponent open={loading} />
            </Box>
        </>
    )
}

export default StudentSchedule
