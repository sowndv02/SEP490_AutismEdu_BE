import CloseIcon from '@mui/icons-material/Close';
import { Box, Button, Checkbox, Divider, FormControl, FormHelperText, IconButton, ListItemText, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';
import WarningIcon from '@mui/icons-material/Warning';
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

const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
    PaperProps: {
        style: {
            maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
            width: 250,
        },
    },
};
function ScheduleCreation({ setListTimeSlots, id, listTimeSlots }) {
    const [open, setOpen] = useState(false);
    const [dayOfWeek, setDayOfWeek] = useState([]);
    const [startTime, setStartTime] = useState("");
    const [endTime, setEndTime] = useState("");
    const [timeError, setTimeError] = useState("");
    const [disableDate, setDisableDate] = useState([]);
    const [existSchedule, setExistSchedule] = useState([]);
    const [listSchedule, setListSchedule] = useState([]);
    const [overlapSchedules, setOverlapSchedules] = useState([]);
    const [existSlots, setExistSlots] = useState([]);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    useEffect(() => {
        getExistSchedule();
        getExistSlot();
    }, [listTimeSlots])
    useEffect(() => {
        getExistSchedule();
    }, [])
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
        setDayOfWeek([])
    }, [startTime, endTime])

    useEffect(() => {
        const disableArr = [];
        existSchedule.forEach((l) => {
            if (toMinutes(l.from) < toMinutes(endTime) && toMinutes(startTime) < toMinutes(l.to)
                && !disableArr.includes(l.weekday)) {
                disableArr.push(l.weekday);
            }
        })
        setDisableDate([...disableArr]);
        setDayOfWeek([])
    }, [existSchedule])

    const getExistSchedule = async () => {
        try {
            await services.StudentProfileAPI.getTutorSchedule((res) => {
                const arr = [];
                res.result.forEach((a) => {
                    a.scheduleTimeSlots.forEach((s) => {
                        arr.push(s);
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
                setExistSlots(res.result.schedules);
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
    const handleChange = (event) => {
        const {
            target: { value },
        } = event;
        setDayOfWeek(
            typeof value === 'string' ? value.split(',') : value,
        );
    };
    const handleAddTime = () => {
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
        else {
            let arrSchedule = [];
            const scheduleItem = dayOfWeek.map((d) => {
                const weekDay = days.find((day) => {
                    return day.day === d;
                })
                const overlapSchedule = existSlots.filter((e) => {
                    let scheduleDate = new Date(e.scheduleDate).getDay();

                    if (weekDay.id === scheduleDate && toMinutes(e.start) < toMinutes(endTime) && toMinutes(startTime) < toMinutes(e.end)) {
                        return e;
                    }
                })
                arrSchedule = [...overlapSchedule, ...arrSchedule];
                return {
                    weekday: days.find((day) => day.day === d).id,
                    from: startTime,
                    to: endTime,
                    status: overlapSchedule.length > 0 ? true : false
                }
            })
            setOverlapSchedules([...arrSchedule, ...overlapSchedules])
            const updatedSchedule = [...scheduleItem, ...listSchedule];
            const sortedItem = updatedSchedule.sort((a, b) => {
                return a.weekday - b.weekday
            })
            setListSchedule(sortedItem);
            setExistSchedule([...existSchedule, ...sortedItem])
            setDayOfWeek([]);
            setTimeError("");
            setStartTime("");
            setEndTime("")
        }
    }

    const handleDeleteSchedule = (index) => {
        const filter = listSchedule.filter((l, i) => {
            return i !== index
        })
        if (overlapSchedules && overlapSchedules.length !== 0) {
            const updateOverlapSchedule = overlapSchedules.filter((o) => {
                const scheduleDate = new Date(o.scheduleDate).getDay();
                if (listSchedule[index].weekday !== scheduleDate
                    || toMinutes(o.start) > toMinutes(listSchedule[index].to)
                    || toMinutes(listSchedule[index].from) > toMinutes(o.end)) {
                    return o;
                }
            })
            setOverlapSchedules([...updateOverlapSchedule]);
        }
        const updateExistSchedule = existSchedule.filter((e) => {
            if (e.weekday !== listSchedule[index].weekday ||
                e.from !== listSchedule[index].from ||
                e.to !== listSchedule[index].to
            ) {
                return e;
            }
        })

        setExistSchedule(updateExistSchedule)
        setListSchedule([...filter]);
    }

    const handleCreateTimeSlot = async () => {
        try {
            await services.TimeSlotAPI.createTimeSlot(id, listSchedule,
                (res) => {
                    const updateTimeSlot = [...listTimeSlots, ...res.result]
                    const sortedItem = updateTimeSlot.sort((a, b) => {
                        return a.weekday - b.weekday
                    })
                    enqueueSnackbar("Tạo khung giờ học mới thành công!", { variant: "success" });
                    setOpen(false);
                    setListTimeSlots(sortedItem);
                    setListSchedule([]);
                    setOverlapSchedules([]);
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
            <Button variant='contained' onClick={handleOpen}>Thêm khung giờ mới</Button>
            <Modal
                open={open}
                onClose={handleClose}
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
                    <Typography variant='h5'>Thêm khung giờ mới</Typography>
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
                            <Typography>Thứ trong tuần</Typography>
                            <FormControl sx={{ width: "240px" }}>
                                <Select
                                    multiple
                                    value={dayOfWeek}
                                    onChange={handleChange}
                                    renderValue={(selected) => selected.join(', ')}
                                    MenuProps={MenuProps}
                                    disabled={startTime === "" || endTime === ""}
                                >
                                    {days.map((day) => (
                                        <MenuItem key={day.id} value={day.day} disabled={disableDate.includes(day.id)}>
                                            <Checkbox checked={dayOfWeek.includes(day.day)} />
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
                    <Button variant='contained' sx={{ mt: 2 }} disabled={startTime === "" || endTime === ""} onClick={handleAddTime}>Thêm</Button>
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
                    <Box sx={{ display: "flex", mt: 3, flexWrap: "wrap", gap: 3 }}>
                        {
                            listSchedule.length !== 0 && listSchedule.map((schedule, index) => {
                                return (
                                    <Box sx={{
                                        display: "flex",
                                        boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
                                        p: 2,
                                        gap: 2, alignItems: "center",
                                        bgcolor: schedule.status ? "#ed6c02" : "white",
                                        color: schedule.status ? "white" : ""
                                    }} key={index}>
                                        <Typography sx={{ fontSize: "12px" }}>{days.find((day) => day.id === schedule.weekday).day}</Typography>
                                        <Divider orientation='vertical' sx={{ bgcolor: "black" }} />
                                        <Typography sx={{ fontSize: "12px" }}>{schedule.from} - {schedule.to}</Typography>
                                        <IconButton onClick={() => handleDeleteSchedule(index)}>
                                            <CloseIcon sx={{ fontSize: "14px" }} />
                                        </IconButton>
                                    </Box>
                                )
                            })
                        }
                    </Box>
                    <Button variant='contained' sx={{ mt: 5 }} onClick={handleCreateTimeSlot}
                        disabled={listSchedule.length === 0}
                    >Thêm khung giờ</Button>
                </Box>
            </Modal>
        </>
    )
}

export default ScheduleCreation
