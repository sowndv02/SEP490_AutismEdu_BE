import CloseIcon from '@mui/icons-material/Close';
import { Box, Button, Card, CardContent, Checkbox, Divider, FormControl, FormHelperText, IconButton, ListItemText, MenuItem, Select, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
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
            width: 250
        }
    }
};
function StudentShedule({ listSchedule, setListSchedule }) {
    const [dayOfWeek, setDayOfWeek] = useState([]);
    const [startTime, setStartTime] = useState("");
    const [endTime, setEndTime] = useState("");
    const [timeError, setTimeError] = useState("");
    const [disableDate, setDisableDate] = useState([]);
    const [existSchedule, setExistSchedule] = useState([]);
    const [overlapSchedules, setOverlapSchedules] = useState([]);
    const [existSlots, setExistSlots] = useState([]);
    useEffect(() => {
        getExistSchedule();
        getExistSlot();
    }, [])
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
        try {
            await services.ScheduleAPI.getSchedule((res) => {
                setExistSlots(res.result.schedules);
            }, (error) => {
                console.log(error);
            }, {
                startDate: `${today.getFullYear()}-${today.getMonth() + 1}-${today.getDate()}`,
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
            target: { value }
        } = event;
        setDayOfWeek(
            typeof value === 'string' ? value.split(',') : value
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
        const filterDisableDate = disableDate.filter((d) => {
            return d !== listSchedule[index].weekday;
        })
        setDisableDate(filterDisableDate)
        setExistSchedule(updateExistSchedule)
        setListSchedule([...filter]);
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
        <Card sx={{ px: 2, mt: 3 }}>
            <CardContent sx={{ px: 0 }}>
                <Typography variant='h5'>Lịch học</Typography>
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
                                labelId="demo-multiple-checkbox-label"
                                id="demo-multiple-checkbox"
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
                    <Stack direction='row' alignItems="center" mt={3}>
                        <WarningAmberIcon sx={{ color: "orange" }} />
                        <Typography sx={{ color: "orange" }}>Lịch của bạn bị trùng</Typography>
                    </Stack>
                }
                <ul style={{ color: "orange" }}>
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
                                    bgcolor: schedule.status ? "orange" : "white",
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
            </CardContent>
        </Card >
    )
}

export default StudentShedule
