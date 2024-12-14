import { Box, Button, FormControl, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { useParams } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { tutorInfor } from '~/redux/features/tutorSlice';
import AssignExercise from './CalendarModal/AssignExercise';
import Evaluate from './CalendarModal/Evaluate';
import ViewDetailModal from './CalendarModal/ViewDetailModal';
import CalenderButtons from './CalenderButtons/CalenderButtons';
import ChangeSlotModal from './ScheduleUpdater';
function Calendar() {
    const { id } = useParams();
    const [isModalOpen, setModalOpen] = useState(false);
    const [isEvaluateModalOpen, setEvaluateModalOpen] = useState(false);
    const [selectedKey, setSelectedKey] = useState('');
    const [aSchedule, setASchedule] = useState(null);
    const tutorInformation = useSelector(tutorInfor);
    const [weekInYears, setWeekInYears] = useState([]);
    const [listYears, setListYears] = useState([]);
    const [currentYear, setCurrentYear] = useState(new Date().getFullYear());
    const [currentWeek, setCurrentWeek] = useState(0);
    const [filterSchedule, setFilterSchedule] = useState(null);
    const [loading, setLoading] = useState(false);
    const [isChange, setIsChange] = useState(true);
    const [isDetailModalOpen, setDetailModalOpen] = useState(false);
    useEffect(() => {
        if (weekInYears.length !== 0) {
            getSchedule();
        }
    }, [weekInYears, isChange, currentWeek])

    const getSchedule = async () => {
        try {
            setLoading(true);
            await services.ScheduleAPI.getSchedule((res) => {
                if (listYears.length !== 0) {
                    const startYear = new Date(tutorInformation.createdDate).getFullYear();
                    const maxYear = new Date(res.result.maxDate).getFullYear();
                    const years = [];
                    for (let year = startYear; year <= maxYear; year++) {
                        years.push(year);
                    }
                    years.reverse();
                    setListYears(years);
                }
                organizeSchedulesByDay(res.result.schedules)
            }, (err) => {
                console.log(err);
            }, {
                studentProfileId: 0,
                startDate: formatDate(weekInYears[currentWeek].monday),
                endDate: formatDate(weekInYears[currentWeek].sunday)
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }

    useEffect(() => {
        if (tutorInformation) {
            const startYear = new Date(tutorInformation.createdDate).getFullYear();
            const currentYear = new Date().getFullYear();
            const years = [];
            for (let year = startYear; year <= currentYear; year++) {
                years.push(year);
            }
            years.reverse();
            setListYears(years);
        }
    }, [tutorInformation])
    useEffect(() => {
        const year = new Date().getFullYear();
        const weeks = generateMondaysAndSundays(year);
        const today = resetTime(new Date());
        const index = weeks.findIndex(week => today >= resetTime(week.monday) && today <= resetTime(week.sunday));
        setCurrentWeek(index);
        setWeekInYears(weeks);
    }, [])

    function generateMondaysAndSundays(year) {
        const result = [];
        let date = new Date(year, 0, 1);
        while (date.getDay() !== 0) {
            date.setDate(date.getDate() + 1);
        }
        let monday = new Date(date);
        monday.setDate(monday.getDate() - 6);

        while (monday.getFullYear() === year || (monday.getFullYear() < year && monday.getMonth() === 11)) {
            const sunday = new Date(monday);
            sunday.setDate(monday.getDate() + 6);
            result.push({
                monday: new Date(monday), sunday: new Date(sunday),
                mondayText: `${String(monday.getDate()).padStart(2, '0')}/${String(monday.getMonth() + 1).padStart(2, '0')}`,
                sundayText: `${String(sunday.getDate()).padStart(2, '0')}/${String(sunday.getMonth() + 1).padStart(2, '0')}`
            });
            monday.setDate(monday.getDate() + 7);
        }
        return result;
    }

    function resetTime(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
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
        return formattedTime;
    };

    const handleAssign = (f, keys) => {
        setSelectedKey(keys);
        setASchedule(f);
        setModalOpen(true);
    };

    const handleOpenEvaluate = (f, keys) => {
        setSelectedKey(keys);
        setASchedule(f);
        setEvaluateModalOpen(true);
    };

    const passStatus = (value) => {
        return value === 2 ? 'Chưa có' : value === 1 ? "Đạt" : "Chưa đạt"
    };
    const attendanceStatus = (value) => {
        return value === 2 ? 'Chưa có mặt' : value === 1 ? "Có mặt" : "Vắng"
    };

    const checkTime = (time) => {
        if (!time) {
            return false;
        }
        const startTime = new Date(time.scheduleDate);
        const endTime = new Date(time.scheduleDate);
        const [startHour, startMinute, startSecond] = time.start.split(":").map(Number);
        const [endHour, endMinute, endSecond] = time.end.split(":").map(Number);

        startTime.setHours(startHour, startMinute, startSecond);
        endTime.setHours(endHour, endMinute, endSecond);
        const now = new Date();
        if (now.getTime() >= startTime.getTime() && now.getTime() <= endTime.getTime()) {
            return true;
        } else if (now.getTime() > endTime.getTime()) {
            return true;
        } else {
            return false;
        }
    };

    const handleViewDetail = (f) => {
        setASchedule(f);
        setDetailModalOpen(true);
    };

    return (
        <>
            <Box p="30px" sx={{ width: "100%", height: "calc(100vh - 64px)" }}>
                <Stack direction='row' alignItems="center" gap={3}>
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
                                        <MenuItem key={l} value={l} >{l}</MenuItem>
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
                                        py: 2,
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
                                                                {
                                                                    !checkTime(f) && <ChangeSlotModal schedule={f} setIsChange={setIsChange} />
                                                                }
                                                                <CalenderButtons f={f} keys={keys} handleAssign={handleAssign} handleOpenEvaluate={handleOpenEvaluate} />
                                                                <Button size='small' variant='contained'
                                                                    sx={{ mt: 2, fontSize: "12px", backgroundColor: '#218eed' }}
                                                                    onClick={() => handleViewDetail(f)}
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
                {isModalOpen && selectedKey && aSchedule && <AssignExercise isOpen={isModalOpen} setModalOpen={setModalOpen} schedule={aSchedule} filterSchedule={filterSchedule} setFilterSchedule={setFilterSchedule} selectedKey={selectedKey} />}
                {isEvaluateModalOpen && aSchedule && <Evaluate isOpen={isEvaluateModalOpen} setModalOpen={setEvaluateModalOpen} schedule={aSchedule} selectedKey={selectedKey} filterSchedule={filterSchedule} setFilterSchedule={setFilterSchedule} />}
                {aSchedule && tutorInformation && <ViewDetailModal isOpen={isDetailModalOpen} setModalOpen={setDetailModalOpen} schedule={aSchedule} setSchedule={setASchedule} tutorName={tutorInformation?.fullName} />}
                <LoadingComponent open={loading} />
            </Box>
        </>
    )
}

export default Calendar