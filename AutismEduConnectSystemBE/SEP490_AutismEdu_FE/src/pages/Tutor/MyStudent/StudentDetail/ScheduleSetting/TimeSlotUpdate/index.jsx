import WarningIcon from '@mui/icons-material/Warning';
import { Box, Button, FormControl, FormHelperText, ListItemText, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';
const days = [
    {
        id: 1,
        day: "Thứ 2"
    },
    {
        id: 2,
        day: "Thứ 3"
    },
    {
        id: 3,
        day: "Thứ 4"
    },
    {
        id: 4,
        day: "Thứ 5"
    },
    {
        id: 5,
        day: "Thứ 6"
    },
    {
        id: 6,
        day: "Thứ 7"
    },
    {
        id: 0,
        day: "Chủ nhật"
    }
];

function TimeSlotUpdate({ setListTimeSlots, selectedTimeSlot, listTimeSlots, open, setOpen }) {
    const [dayOfWeek, setDayOfWeek] = useState(0);
    const [startTime, setStartTime] = useState("");
    const [endTime, setEndTime] = useState("");
    const [timeError, setTimeError] = useState("");
    const [disableDate, setDisableDate] = useState([]);
    const [existSchedule, setExistSchedule] = useState([]);
    const [overlapSchedules, setOverlapSchedules] = useState([]);
    const [existSlots, setExistSlots] = useState([]);
    const [change, setChange] = useState(true);
    useEffect(() => {
        if (open) {
            getExistSchedule();
            getExistSlot();
            setChange(true);
        } else {
            setExistSchedule([]);
            setExistSlots([]);
            setOverlapSchedules([]);
        }
    }, [open])
    useEffect(() => {
        getExistSchedule();
        if (selectedTimeSlot) {
            const start = selectedTimeSlot.from.split(":");
            const end = selectedTimeSlot.to.split(":");
            setStartTime(start[0] + ":" + start[1]);
            setEndTime(end[0] + ":" + end[1]);
            const getDay = days.find((d) => {
                return selectedTimeSlot.weekday === d.id
            })
            setDayOfWeek(getDay.day);
        }
    }, [selectedTimeSlot])
    useEffect(() => {
        if (!open) {
            setStartTime("");
            setEndTime("");
        }
    }, [open])

    useEffect(() => {
        const disableArr = [];
        existSchedule.forEach((l) => {
            if (toMinutes(l.from) < toMinutes(endTime) && toMinutes(startTime) < toMinutes(l.to)
                && !disableArr.includes(l.weekday)) {
                disableArr.push(l.weekday);
            }
        })
        setDisableDate([...disableArr]);
    }, [startTime, endTime, existSchedule])

    useEffect(() => {
        if (dayOfWeek !== 0) {
            const weekDay = days.find((day) => {
                return day.day === dayOfWeek;
            })
            const overlapSchedule = existSlots.filter((e) => {
                let scheduleDate = new Date(e.scheduleDate).getDay();

                if (weekDay.id === scheduleDate && toMinutes(e.start) < toMinutes(endTime) && toMinutes(startTime) < toMinutes(e.end)) {
                    return e;
                }
            })
            setOverlapSchedules([...overlapSchedule])
        } else {
            setOverlapSchedules([]);
        }
    }, [dayOfWeek])
    const getExistSchedule = async () => {
        try {
            await services.StudentProfileAPI.getTutorSchedule((res) => {
                const arr = [];
                res.result.forEach((a) => {
                    a.scheduleTimeSlots.forEach((s) => {
                        if (selectedTimeSlot.id !== s.id) {
                            arr.push(s);
                        }
                    })
                })
                setExistSchedule(arr);
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }

    const getExistSlot = async () => {
        const today = new Date();
        const daysUntilNextMonday = (8 - today.getDay()) % 7 || 7;
        const nextMonday = new Date(today);
        nextMonday.setDate(today.getDate() + daysUntilNextMonday);
        try {
            await services.ScheduleAPI.getSchedule((res) => {
                const filterSlot = res.result.schedules.filter((f) => {
                    return f.isUpdatedSchedule === true
                })
                setExistSlots(filterSlot);
            }, (error) => {
                console.log(error);
            }, {
                startDate: `${nextMonday.getFullYear()}-${nextMonday.getMonth() + 1}-${nextMonday.getDate()}`,
                studentProfileId: 0
            })
        } catch (error) {
            console.log(error);
        }
    }
    const toMinutes = (time) => {
        const [hours, minutes] = time.split(':').map(Number);
        return hours * 60 + minutes;
    };

    const handleChange = (e) => {
        if (e.target.value === 0) {
            setDayOfWeek(0);
            return;
        }
        const selectedDay = days.find((d) => {
            return d.day === e.target.value;
        })

        const start = selectedTimeSlot.from.split(":");
        const end = selectedTimeSlot.to.split(":");
        const startString = start[0] + ":" + start[1];
        const endString = end[0] + ":" + end[1]
        if ((toMinutes(startString) === toMinutes(startTime)
            && toMinutes(endString) === toMinutes(endTime) && selectedDay.id === selectedTimeSlot.weekday) ||
            e.target.value === 0) {
            setChange(true);
            setOverlapSchedules([])
        } else {
            setChange(false);
        }
        setDayOfWeek(selectedDay.day)
    }
    const handleUpdateTimeSlot = async () => {
        if (startTime === "" || endTime === "" || dayOfWeek.length === 0) {
            setTimeError("Nhập đầy đủ thông tin!");
            return;
        } else if (toMinutes(startTime) >= toMinutes(endTime)) {
            setTimeError("Thời gian không hợp lệ");
            return;
        } else if (toMinutes(endTime) - toMinutes(startTime) < 30) {
            setTimeError("1 buổi học dài ít nhất 30 phút");
            return;
        }
        const selectedDay = days.find((d) => {
            return d.day === dayOfWeek;
        })
        try {
            await services.TimeSlotAPI.updateSlot({
                to: endTime,
                from: startTime,
                weekday: selectedDay.id,
                timeSlotId: selectedTimeSlot.id
            },
                (res) => {
                    const filterSlot = listTimeSlots.filter((l) => {
                        return l.id !== selectedTimeSlot.id;
                    })
                    const updateTimeSlot = [...filterSlot, res.result]
                    const sortedItem = updateTimeSlot.sort((a, b) => {
                        return a.weekday - b.weekday
                    })
                    setOpen(false);
                    setListTimeSlots(sortedItem);
                    enqueueSnackbar("Cập nhật thành công!", { variant: "success" })
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                }
            )
        } catch (error) {
            console.log(error);
        }
    }
    const formatDate = (d) => {
        if (!d) {
            return "";
        }
        const date = new Date(d.scheduleDate);
        const start = d.start.split(":");
        const end = d.end.split(":");
        return `(${start[0]}:${start[1]} - ${end[0]}:${end[1]}) ${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`
    }
    return (
        <>
            <Modal
                open={open}
                onClose={() => setOpen(false)}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 800,
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    p: 4
                }}>
                    <Typography variant='h5'>Cập nhật khung giờ</Typography>
                    <Typography>Giờ hiện tại: {selectedTimeSlot?.from} - {selectedTimeSlot?.to} - {
                        days.find((d) => {
                            return d.id === selectedTimeSlot?.weekday
                        })?.day
                    }</Typography>
                    <Box sx={{ display: "flex", gap: 3, mt: 2 }}>
                        <Box>
                            <Typography>Giờ bắt đầu</Typography>
                            <TextField type='time' value={startTime}
                                onChange={(e) => { setStartTime(e.target.value); setDayOfWeek(0) }} />
                        </Box>
                        <Box>
                            <Typography>Giờ kết thúc</Typography>
                            <TextField type='time'
                                value={endTime}
                                onChange={(e) => { setEndTime(e.target.value); setDayOfWeek(0) }}
                            />
                        </Box>
                        <Box>
                            <Typography>Thứ trong tuần</Typography>
                            <FormControl sx={{ width: "240px" }}>
                                <Select
                                    value={dayOfWeek}
                                    onChange={handleChange}
                                    disabled={startTime === "" || endTime === ""}
                                >
                                    <MenuItem value={0}>
                                        <ListItemText primary={"Chọn thứ"} />
                                    </MenuItem>
                                    {days.map((day) => (
                                        <MenuItem key={day.id} value={day.day} disabled={disableDate.includes(day.id)}>
                                            <ListItemText primary={day.day} />
                                        </MenuItem>
                                    ))}
                                </Select>
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
                    {
                        overlapSchedules && overlapSchedules.length !== 0 &&
                        <Stack direction='row' mt={3} gap={2}>
                            <WarningIcon color='warning' />
                            <Typography sx={{ color: "#ed6c02" }}>Lịch của bạn bị trùng</Typography>
                        </Stack>
                    }
                    <ul style={{ color: "#ed6c02" }}>
                        {
                            overlapSchedules && overlapSchedules.length !== 0 && overlapSchedules.map((o) => {
                                return (
                                    <li key={o.id}>{formatDate(o)}</li>
                                )
                            })
                        }
                    </ul>
                    <Button variant='contained' sx={{ mt: 5 }} disabled={change} onClick={handleUpdateTimeSlot}>Cập nhật khung giờ</Button>
                </Box>
            </Modal>
        </>
    )
}

export default TimeSlotUpdate
