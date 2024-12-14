import { Box, Button, FormControl, FormHelperText, Modal, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';

function ScheduleUpdater({ schedule, setIsChange }) {
    const [open, setOpen] = useState(false);
    const [date, setDate] = useState("");
    const [startTime, setStartTime] = useState("");
    const [endTime, setEndTime] = useState("");
    const [timeError, setTimeError] = useState("");
    const [existSchedule, setExistSchedule] = useState([]);
    const [schedules, setSchedules] = useState([]);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        getExistSchedule();
    }, [])
    useEffect(() => {
        if (open) {
            getExistSchedule();
        }
    }, [open])

    useEffect(() => {
        checkSelectedTime();
    }, [startTime, endTime, date])

    const checkSelectedTime = () => {
        if (startTime === "" || endTime === "" || date === "") {
            setTimeError("Nhập đầy đủ thông tin!");
            return false;
        } else if (startTime >= endTime) {
            setTimeError("Thời gian không hợp lệ!");
            return false;
        } else if (toMinutes(endTime) - toMinutes(startTime) < 30) {
            setTimeError("1 buổi học dài ít nhất 30 phút!");
            return;
        }

        const [hours, minutes] = startTime.split(':').map(Number);
        const today = new Date();
        const selectedDate = new Date(date);
        const currentHours = today.getHours();
        const currentMinutes = today.getMinutes();
        if (today.getFullYear() === selectedDate.getFullYear() && today.getMonth() === selectedDate.getMonth()
            && today.getDate() === selectedDate.getDate()) {
            if (hours < currentHours || (hours === currentHours && minutes < currentMinutes)) {
                setTimeError("Thời gian đã nhập nhỏ hơn thời gian hiện tại.!");
                return;
            }
        }
        let check = false;
        const scheduleInDate = existSchedule.filter((c) => {
            const d = new Date(c.scheduleDate);
            return d.getFullYear() === selectedDate.getFullYear() && d.getMonth() === selectedDate.getMonth()
                && d.getDate() === selectedDate.getDate();
        })
        scheduleInDate.forEach((s) => {
            if (s.id !== schedule.id && toMinutes(s.start) < toMinutes(endTime) && toMinutes(startTime) < toMinutes(s.end)) {
                check = true;
            }
        })
        if (check) {
            setTimeError("Thời gian bị trùng lịch khác!");
            return;
        }
        setTimeError("");
        return true;
    }
    useEffect(() => {
        if (!open) {
            setStartTime("");
            setEndTime("");
        }
    }, [open])

    useEffect(() => {
        checkSelectedTime();
    }, [schedules])
    const getExistSchedule = async () => {
        try {
            setLoading(true);
            await services.ScheduleAPI.getSchedule((res) => {
                setExistSchedule(res.result.schedules)
            }, (err) => {
                console.log(err);
            }, {
                startDate: date,
                getAll: false
            })
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    }
    const getMinDate = () => {
        const newDate = new Date();
        const year = newDate.getFullYear();
        const month = String(newDate.getMonth() + 1).padStart(2, '0');
        const day = String(newDate.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    const getMaxDate = () => {
        if (!schedule) return "";
        const maxDate = new Date();
        maxDate.setDate(maxDate.getDate() + 20);
        const year = maxDate.getFullYear();
        const month = String(maxDate.getMonth() + 1).padStart(2, '0');
        const day = String(maxDate.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    const getDate = () => {
        if (!schedule) return ""
        const date = new Date(schedule.scheduleDate);
        const start = schedule.start.split(":");
        const end = schedule.end.split(":");
        return `(${start[0]}:${start[1]} - ${end[0]}:${end[1]})  ${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`
    }

    const toMinutes = (time) => {
        const [hours, minutes] = time.split(':').map(Number);
        return hours * 60 + minutes;
    };
    const handleChangeSchedule = async () => {
        const checkValidate = checkSelectedTime();
        if (!checkValidate) {
            return;
        }
        try {
            await services.TimeSlotAPI.updateTimeSlot({
                id: schedule.id,
                scheduleDate: date,
                start: startTime,
                end: endTime
            }, (res) => {
                setSchedules([]);
                setStartTime("")
                setEndTime("");
                setDate("");
                handleClose();
                setIsChange(pre => !pre)
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
        } catch (error) {
            console.log(error);
        }
    }

    // const checkTime = () => {
    //     const startDate = new Date(date);
    //     const endDate = new Date(date);
    //     const [startHour, startMinute] = startTime.split(":").map(Number);
    //     const [endHour, endMinute] = endTime.split(":").map(Number);
    //     startDate.setHours(startHour, startMinute, "00");
    //     endDate.setHours(endHour, endMinute, "00");
    //     const now = new Date();
    //     if (now >= startDate && now <= endDate) {
    //         setTimeError("Thời gian không hợp lệ!");
    //         return true;
    //     } else if (now > endDate) {
    //         setTimeError("Thời gian không hợp lệ!");
    //         return true;
    //     } else {
    //         return false;
    //     }
    // }
    return (
        <>
            <Button sx={{ mt: 2, fontSize: "12px", borderColor: '#218eed' }} variant='outlined' size='medium' onClick={handleOpen}>Đổi lịch</Button>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 600,
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    p: 4
                }}>
                    <Typography variant='h5'>Đổi thời gian học</Typography>
                    {
                        schedule && (
                            <Box mt={3}>
                                <Typography>Đổi thời gian học của <strong>{schedule.studentProfile.name}</strong></Typography>
                                <Typography color='green'>{getDate()}</Typography>
                            </Box>
                        )
                    }
                    <Box sx={{ display: "flex", gap: 3, mt: 2 }}>
                        <Box>
                            <Typography>Giờ bắt đầu</Typography>
                            <TextField type='time' value={startTime}
                                onChange={(e) => setStartTime(e.target.value)} />
                        </Box>
                        <Box>
                            <Typography>Giờ kết thúc</Typography>
                            <TextField type='time'
                                value={endTime}
                                onChange={(e) => setEndTime(e.target.value)}
                            />
                        </Box>
                        <Box>
                            <Typography>Ngày học mới</Typography>
                            <FormControl sx={{ width: "240px" }}>
                                <TextField type='date' value={date}
                                    onChange={(e) => { setDate(e.target.value) }} inputProps={{
                                        min: getMinDate(),
                                        max: getMaxDate()
                                    }} />
                            </FormControl>
                        </Box>
                    </Box>
                    {
                        timeError !== "" && (
                            <FormHelperText error>
                                {timeError}
                            </FormHelperText>
                        )
                    }
                    <Button variant='contained' sx={{ mt: 5 }} onClick={handleChangeSchedule}>Đổi lịch</Button>
                    <LoadingComponent open={loading} />
                </Box>
            </Modal>
        </>
    )
}

export default ScheduleUpdater
