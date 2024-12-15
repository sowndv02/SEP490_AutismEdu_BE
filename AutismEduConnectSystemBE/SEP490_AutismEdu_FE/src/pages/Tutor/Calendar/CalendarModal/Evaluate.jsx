import React, { useEffect, useState } from 'react';
import {
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    FormControl,
    FormControlLabel,
    FormHelperText,
    FormLabel,
    Grid,
    Modal,
    Radio,
    RadioGroup,
    Stack,
    TextField,
    Typography
} from '@mui/material';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';
import { formatDate } from 'date-fns';

function Evaluate({ isOpen, setModalOpen, schedule, selectedKey, filterSchedule, setFilterSchedule }) {
    console.log(schedule);

    const [loading, setLoading] = useState(false);
    const [attendance, setAttendance] = useState(0);
    const [evaluation, setEvaluation] = useState(0);
    const [note, setNote] = useState('');
    const [notificationModalOpen, setNotificationModalOpen] = useState(false);
    const [notificationMessage, setNotificationMessage] = useState('');
    const [isValidate, setValidate] = useState(true);
    const [openDialog, setOpenDialog] = useState(false);
    const [selectedContent, setSelectedContent] = useState('');
    const [isDisabled, setIsDisabled] = useState(true);
    const [errors, setErrors] = useState({});

    const validateForm = () => {

        const newErrors = {};

        if (note.trim().length > 500) {
            newErrors.note = 'Không được vượt quá 500 ký tự';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    useEffect(() => {
        setIsDisabled(!validateForm());
    }, [note]);

    useEffect(() => {
        if (schedule) {
            setAttendance(schedule.attendanceStatus === 2 ? 0 : schedule.attendanceStatus);
            setEvaluation(schedule.passingStatus === 2 ? 0 : schedule.passingStatus);
            setNote(schedule.note || '');
        }
    }, [schedule]);

    const onClose = () => {
        setAttendance(0);
        setEvaluation(0);
        setNote('');
        setModalOpen(false);
    };

    const handleSubmit = async () => {
        try {
            setLoading(true);
            const data = {
                "id": schedule?.id,
                "attendanceStatus": attendance,
                "passingStatus": evaluation,
                note: note?.trim()
            };
            await services.ScheduleAPI.updateScheduleChangeStatus(schedule?.id, data, (res) => {

                if (res?.result) {
                    const updateData = filterSchedule[selectedKey].map((s) => {
                        if (s.id === res.result?.id) {
                            s = res.result;
                            return s;
                        } else {
                            return s;
                        }
                    });

                    setFilterSchedule((prev) => ({ ...prev, [selectedKey]: updateData }));
                    enqueueSnackbar("Đánh giá thành công!", { variant: 'success' });
                    onClose();
                }
                if (res?.errorMessages?.length !== 0) {
                    setNotificationMessage(res?.errorMessages[0] || '');
                    setNotificationModalOpen(true);
                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        setValidate(
            schedule?.attendanceStatus === attendance &&
            schedule?.passingStatus === evaluation &&
            (schedule?.note || '') === note
        );
    }, [attendance, evaluation, note, schedule]);

    function formatTime(timeString) {
        const [hours, minutes] = timeString.split(":");
        return `${hours}:${minutes}`;
    }

    const handleOpenDialog = (content) => {
        if (!content) {
            enqueueSnackbar('Không có chi tiết bài tập!', { variant: 'error' });
            return;
        }
        setSelectedContent(content);
        setOpenDialog(true);
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
        setSelectedContent('');
    };

    const handleAttendance = (e) => {
        setAttendance(Number(e.target.value));
        setEvaluation(0);
    };


    return (
        <>
            <Modal open={isOpen} onClose={onClose}>
                <Box sx={{
                    position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
                    width: 600, bgcolor: 'background.paper', p: 4, boxShadow: 24, borderRadius: 2,
                    outline: 'none'
                }}>
                    {loading && (
                        <LoadingComponent open={loading} />
                    )}
                    <Typography variant="h5" sx={{ mb: 2, textAlign: 'center', fontWeight: '600' }}>Đánh giá buổi học</Typography>
                    <Divider sx={{ mb: 3 }} />

                    <Grid container spacing={2} sx={{ mb: 3 }}>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Mã học sinh:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{schedule.studentProfile?.studentCode}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Tên học sinh:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'><em>{schedule.studentProfile?.name}</em></Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Ngày học:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{schedule?.scheduleDate ? formatDate(new Date(schedule?.scheduleDate), "dd/MM/yyyy") : 'Chưa có'}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Khung thời gian:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{formatTime(schedule.start)} - {formatTime(schedule.end)}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Loại bài tập:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{schedule?.exerciseType?.exerciseTypeName || '-'}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Bài tập:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>
                                {schedule?.exercise?.exerciseName || '-'}
                            </Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Button variant='text' sx={{ padding: 0 }} onClick={() => handleOpenDialog(schedule?.exercise?.description)}>Xem chi tiết bài tập</Button>
                        </Grid>
                    </Grid>

                    <Divider sx={{ my: 3 }} />

                    <Stack direction="row" spacing={5} sx={{ mb: 3 }}>
                        <Box>
                            <FormControl component="fieldset">
                                <FormLabel id="demo-radio-buttons-group-label">Điểm danh</FormLabel>
                                <RadioGroup
                                    row
                                    value={attendance}
                                    onChange={(e) => handleAttendance(e)}
                                >
                                    <FormControlLabel value={1} control={<Radio />} label="Có mặt" />
                                    <FormControlLabel value={0} control={<Radio />} label="Vắng" />
                                </RadioGroup>
                            </FormControl>
                        </Box>
                        <Box>
                            <FormControl component="fieldset">
                                <FormLabel id="demo-radio-buttons-group-label2">Đánh giá</FormLabel>
                                <RadioGroup
                                    row
                                    value={evaluation}
                                    onChange={(e) => setEvaluation(Number(e.target.value))}
                                >
                                    <FormControlLabel value={1} control={<Radio disabled={attendance === 0} />} label="Đạt" />
                                    <FormControlLabel value={0} control={<Radio disabled={attendance === 0} />} label="Chưa đạt" />
                                </RadioGroup>
                            </FormControl>
                        </Box>
                    </Stack>

                    <Stack direction="column" spacing={1} sx={{ mb: 3 }}>
                        <TextField
                            label="Ghi chú"
                            multiline
                            rows={4}
                            value={note}
                            onChange={(e) => setNote(e.target.value)}
                            variant="outlined"
                            fullWidth
                        />
                        {errors.note ? (
                            <FormHelperText error>{errors.note}</FormHelperText>
                        ) : <Typography variant='caption'>{note?.trim()?.length}/500</Typography>}
                    </Stack>

                    <Stack direction="row" spacing={2} justifyContent="flex-end">
                        <Button variant="outlined" onClick={onClose} sx={{ px: 3 }}>Huỷ</Button>
                        <Button variant="contained" color="primary" onClick={handleSubmit} sx={{ px: 3 }} disabled={isValidate||isDisabled}>Lưu</Button>
                    </Stack>
                </Box>
            </Modal >
            <Modal open={notificationModalOpen} onClose={() => setNotificationModalOpen(false)}>
                <Box sx={{
                    position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
                    width: 400, bgcolor: 'background.paper', p: 4, boxShadow: 24, borderRadius: 2,
                    outline: 'none', textAlign: 'center'
                }}>
                    <Typography variant="h6" sx={{ mb: 2, fontWeight: '600' }}>Thông báo</Typography>
                    <Divider sx={{ mb: 3 }} />
                    <Typography variant="body1" sx={{ mb: 3 }}>{notificationMessage}</Typography>
                    <Button variant="contained" onClick={() => setNotificationModalOpen(false)} >Xác nhận</Button>
                </Box>
            </Modal>
            {selectedContent && <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
                <DialogTitle textAlign={'center'}>Nội dung bài tập</DialogTitle>
                <Divider />
                <DialogContent>
                    <Box mx={'auto'} width={'90%'} dangerouslySetInnerHTML={{ __html: selectedContent }} />
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={handleCloseDialog} variant='contained' color="primary">Đóng</Button>
                </DialogActions>
            </Dialog>}
        </>
    );
}

export default Evaluate;
