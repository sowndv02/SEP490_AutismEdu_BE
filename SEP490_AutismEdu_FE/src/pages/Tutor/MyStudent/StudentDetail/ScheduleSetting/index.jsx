import CloseIcon from '@mui/icons-material/Close';
import PriorityHighIcon from '@mui/icons-material/PriorityHigh';
import { Box, IconButton, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import ConfirmDialog from '~/components/ConfirmDialog';
import services from '~/plugins/services';
import CreateSchedule from './ScheduleCreation';
import { enqueueSnackbar } from 'notistack';
import BorderColorIcon from '@mui/icons-material/BorderColor';
import UpdateSchedule from './TimeSlotUpdate';
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
function ScheduleSetting({ studentProfile }) {
    const [openConfirm, setOpenConfirm] = useState(false);
    const [listTimeSlots, setListTimeSlots] = useState([]);
    const [selectedTimeSlot, setSelectedTimeSlot] = useState(null);
    const [openEdit, setOpenEdit] = useState(false);
    useEffect(() => {
        handleGetTimeSLots();
    }, [])
    const handleGetTimeSLots = async () => {
        try {
            await services.TimeSlotAPI.getStudentTimeSlot(studentProfile?.id,
                (res) => {
                    const sortedItem = res.result.scheduleTimeSlots.sort((a, b) => {
                        return a.weekday - b.weekday
                    })
                    setListTimeSlots(sortedItem)
                    setSelectedTimeSlot(res.result.scheduleTimeSlots[0])
                }, (error) => {
                    console.log(error);
                })
        } catch (error) {
            console.log(error);
        }
    }

    const formatTime = (time) => {
        if (!time) return "";
        const splitTime = time.split(":");
        return `${splitTime[0]}:${splitTime[1]}`
    }

    const handleDelete = async () => {
        try {
            await services.TimeSlotAPI.deleteTimeSlot(selectedTimeSlot.id,
                (res) => {
                    const filterTimeSlot = listTimeSlots.filter((l) => {
                        return l.id !== selectedTimeSlot.id;
                    })
                    setListTimeSlots(filterTimeSlot);
                    setOpenConfirm(false);
                    enqueueSnackbar("Xoá lịch thành công", { variant: 'success' })
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                }
            )
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Box sx={{ px: 5, py: 3 }}>
            <Typography variant='h4'>Cài đặt lịch học</Typography>
            <Stack direction='row' mt={5}>
                <Box sx={{ width: "70%" }}>
                    <CreateSchedule listTimeSlots={listTimeSlots} setListTimeSlots={setListTimeSlots} id={studentProfile?.id} />
                    <Stack direction='row' gap={3} flexWrap='wrap'>
                        {
                            listTimeSlots.length !== 0 && listTimeSlots.map((l) => {
                                return (
                                    <Box key={l.id} sx={{
                                        display: "flex",
                                        boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
                                        p: 2,
                                        gap: 2, alignItems: "center",
                                        justifyContent: "space-between",
                                        width: "40%",
                                        mt: 3,
                                    }}>
                                        <Typography sx={{ fontSize: "16px" }}>{days.find((day) => day.id === l.weekday).day}</Typography>
                                        <Typography sx={{ fontSize: "16px" }}>{formatTime(l.from)} - {formatTime(l.to)}</Typography>
                                        <IconButton onClick={() => { setOpenEdit(true); setSelectedTimeSlot(l) }}>
                                            <BorderColorIcon sx={{ fontSize: "26px", color: "orange" }} />
                                        </IconButton>
                                        <IconButton onClick={() => { setOpenConfirm(true); setSelectedTimeSlot(l) }}>
                                            <CloseIcon sx={{ fontSize: "26px", color: "red" }} />
                                        </IconButton>
                                    </Box>
                                )
                            })
                        }
                    </Stack>
                </Box>
                <Box sx={{ width: "30%" }}>
                    <Stack direction="row">
                        <PriorityHighIcon sx={{ color: "red" }} />
                        <Typography variant='h6'>Lưu ý</Typography>
                    </Stack>
                    <ul>
                        <li>Lịch học chỉ được cập nhật ở các tuần tiếp theo</li>
                    </ul>
                </Box>
            </Stack >
            <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm}
                title={"Xoá khung giờ"}
                handleAction={handleDelete}
                content={`Bạn có muốn xoá khung giờ ${formatTime(selectedTimeSlot?.from)} - ${formatTime(selectedTimeSlot?.to)} ${days.find((day) => day.id === selectedTimeSlot?.weekday)?.day}?`}
            />
            <UpdateSchedule listTimeSlots={listTimeSlots} selectedTimeSlot={selectedTimeSlot} setListTimeSlots={setListTimeSlots}
                open={openEdit} setOpen={setOpenEdit} />
        </Box >
    )
}

export default ScheduleSetting
