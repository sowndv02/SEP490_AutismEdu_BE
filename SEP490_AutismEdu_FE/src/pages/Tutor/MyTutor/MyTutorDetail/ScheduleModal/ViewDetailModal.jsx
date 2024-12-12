import React, { useEffect, useState } from 'react';
import {
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    Grid,
    Modal,
    Stack,
    Typography
} from '@mui/material';
import { enqueueSnackbar } from 'notistack';

function ViewDetailModal({ isOpen, setModalOpen, schedule, setSchedule, tutorName }) {

    const [selectedContent, setSelectedContent] = useState('');
    const [openDialog, setOpenDialog] = useState(false);
    const [selectedContentE, setSelectedContentE] = useState('');
    const [openDialogE, setOpenDialogE] = useState(false);

    const handleOpenDialogE = (content) => {
        if (!content) {
            enqueueSnackbar('Không có chi tiết bài tập!', { variant: 'error' });
            return;
        }
        setSelectedContentE(content);
        setOpenDialogE(true);
    };

    const handleCloseDialogE = () => {
        setOpenDialogE(false);
        setSelectedContentE('');
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
        setSelectedContent('');
    };

    const handleOpenDialog = (content) => {
        setSelectedContent(content);
        setOpenDialog(true);
    };

    const onClose = () => {
        setSchedule(null);
        setModalOpen(false);
    };

    function formatTime(timeString) {
        const [hours, minutes] = timeString.split(":");
        return `${hours}:${minutes}`;
    }

    const passStatus = (value) => {

        return value === 2 ? 'Chưa có' : value === 1 ? "Đạt" : "Chưa đạt"
    };
    const attendanceStatus = (value) => {
        return value === 2 ? 'Chưa có mặt' : value === 1 ? "Có mặt" : "Vắng"
    };


    return (
        <>
            <Modal open={isOpen} onClose={onClose}>
                <Box sx={{
                    position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
                    width: 600, bgcolor: 'background.paper', p: 4, boxShadow: 24, borderRadius: 2,
                    outline: 'none'
                }}>
                    <Typography variant="h5" sx={{ mb: 2, textAlign: 'center', fontWeight: '600' }}>Chi tiết về buổi học</Typography>
                    <Divider sx={{ mb: 3 }} />

                    <Grid container spacing={2} sx={{ mb: 3 }}>
                        <Grid item xs={5}>
                            <Typography variant='h6' sx={{ fontWeight: '500' }}>Mã học sinh:</Typography>
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
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Tên gia sư:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'><em>{tutorName ?? 'K'}</em></Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Ngày học:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{new Date(schedule?.scheduleDate)?.toLocaleDateString()}</Typography>
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
                            <Typography variant='subtitle1'>{schedule?.exerciseType?.exerciseTypeName ?? '-'}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Bài tập:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Typography variant='subtitle1'>{schedule?.exercise?.exerciseName ?? '-'}</Typography>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Điểm danh:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Button size='small' variant='outlined' color={schedule?.attendanceStatus === 2 ? 'warning' : schedule?.attendanceStatus === 1 ? 'success' : 'error'}>{attendanceStatus(schedule?.attendanceStatus)}</Button>
                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Đánh giá:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            <Button size='small' variant='outlined' color={schedule?.passingStatus === 1 ? 'success' : schedule?.passingStatus === 2 ? 'warning' : 'error'}>{passStatus(schedule?.passingStatus)}</Button>

                        </Grid>
                        <Grid item xs={5}>
                            <Typography variant='subtitle1' sx={{ fontWeight: '500' }}>Ghi chú:</Typography>
                        </Grid>
                        <Grid item xs={7}>
                            {schedule?.note ? (
                                <Box sx={{ maxWidth: '600px', overflow: 'hidden', wordBreak: 'break-word' }}>
                                    <Typography
                                        variant='subtitle1'
                                        sx={{
                                            display: '-webkit-box',
                                            WebkitBoxOrient: 'vertical',
                                            WebkitLineClamp: 2,
                                            overflow: 'hidden',
                                            whiteSpace: 'normal',
                                            lineHeight: 1.5,
                                        }}
                                    >
                                        {schedule?.note}
                                    </Typography>

                                    {schedule?.note.length > 83 && (
                                        <Typography
                                            variant='body2'
                                            component='span'
                                            onClick={() => handleOpenDialog(schedule?.note)}
                                            sx={{
                                                color: 'blue',
                                                cursor: 'pointer',
                                                marginLeft: '5px',
                                                textDecoration: 'underline',
                                            }}
                                        >
                                            Xem thêm
                                        </Typography>
                                    )}
                                </Box>
                            ) : '-'}
                        </Grid>
                        <Grid item xs={5}>
                            <Button variant='text' sx={{ padding: 0 }} onClick={() => handleOpenDialogE(schedule?.exercise?.description)}>Xem chi tiết bài tập</Button>
                        </Grid>

                    </Grid>

                    <Stack direction="row" spacing={2} justifyContent="flex-end">
                        <Button variant="contained" onClick={onClose} sx={{ px: 3 }}>Quay lại</Button>
                    </Stack>
                </Box>
            </Modal>

            <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
                <DialogTitle textAlign={'center'}>Ghi chú</DialogTitle>
                <DialogContent>
                    <Typography>{selectedContent}</Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseDialog} variant='contained' color="primary">Đóng</Button>
                </DialogActions>
            </Dialog>

            {selectedContentE && <Dialog open={openDialogE} onClose={handleCloseDialogE} maxWidth="md" fullWidth>
                <DialogTitle textAlign={'center'}>Nội dung bài tập</DialogTitle>
                <Divider />
                <DialogContent>
                    <Box mx={'auto'} width={'90%'} dangerouslySetInnerHTML={{ __html: selectedContentE }} />
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={handleCloseDialogE} variant='contained' color="primary">Đóng</Button>
                </DialogActions>
            </Dialog>}

        </>
    );
}

export default ViewDetailModal
