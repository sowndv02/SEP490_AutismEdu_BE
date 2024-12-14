import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, Select, MenuItem, Button, Stack, Typography, Grid, IconButton, Box } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';

function CreateStudentRecordModal({ open, onClose, request }) {
    const [initialStatus, setInitialStatus] = useState('');
    const [evaluation, setEvaluation] = useState(1);
    const [schedule, setSchedule] = useState({
        1: [],
        2: [],
        3: [],
        4: [],
        5: [],
        6: [],
        7: [],
    });
    const [selectedDay, setSelectedDay] = useState(1);
    const [startTime, setStartTime] = useState('');
    const [endTime, setEndTime] = useState('');

    const handleAddTimeSlot = (day) => {
        if (startTime && endTime) {
            setSchedule((prev) => ({
                ...prev,
                [day]: [...prev[day], { startTime, endTime }]
            }));
            setStartTime('');
            setEndTime('');
        }
    };

    const handleDeleteTimeSlot = (day, index) => {
        setSchedule((prev) => ({
            ...prev,
            [day]: prev[day].filter((_, i) => i !== index)
        }));
    };

    const handleSave = () => {
        onClose();
    };

    const daysOfWeek = [
        { label: 'Thứ 2', value: 1 },
        { label: 'Thứ 3', value: 2 },
        { label: 'Thứ 4', value: 3 },
        { label: 'Thứ 5', value: 4 },
        { label: 'Thứ 6', value: 5 },
        { label: 'Thứ 7', value: 6 },
        { label: 'Chủ nhật', value: 7 },
    ];

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle sx={{ backgroundColor: '#556cd6', color: '#fff', textAlign: 'center', fontWeight: 'bold' }}>
                Tạo sổ hồ sơ học sinh
            </DialogTitle>
            <DialogContent sx={{ padding: '20px', backgroundColor: '#f9f9f9' }}>
                <Stack spacing={3}>
                    <Grid container spacing={2} alignItems="center">
                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Tên phụ huynh:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.parentName} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Số điện thoại:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.phone} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Tên trẻ:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.childName} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Giới tính:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.gender} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Ngày sinh:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.birthDate.toLocaleDateString()} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Tuổi:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField fullWidth value={request.childInfo.age} InputProps={{ readOnly: true }} />
                        </Grid>

                        <Grid item xs={4}>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>Tình trạng ban đầu:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                fullWidth
                                multiline
                                rows={4}
                                value={initialStatus}
                                onChange={(e) => setInitialStatus(e.target.value)}
                            />
                        </Grid>
                    </Grid>

                    {/* Đánh giá ban đầu */}
                    <Typography variant="h6" sx={{ fontWeight: 'bold', mt: 3 }}>Đánh giá ban đầu:</Typography>
                    <Grid container spacing={2} alignItems="center">
                        <Grid item xs={4}>
                            <Typography variant="body1">1. Về quan hệ với mọi người:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Select fullWidth value={evaluation} onChange={(e) => setEvaluation(e.target.value)}>
                                <MenuItem value={1}>1 điểm</MenuItem>
                                <MenuItem value={2}>2 điểm</MenuItem>
                                <MenuItem value={3}>3 điểm</MenuItem>
                                <MenuItem value={4}>4 điểm</MenuItem>
                            </Select>
                        </Grid>
                    </Grid>

                    {/* Tạo lịch học */}
                    <Typography variant="h6">Tạo lịch học:</Typography>
                    <Stack direction="row" spacing={2}>
                        {daysOfWeek.map((day) => (
                            <Button key={day.value} variant={selectedDay === day.value ? 'contained' : 'outlined'} onClick={() => setSelectedDay(day.value)}>
                                {day.label}
                            </Button>
                        ))}
                    </Stack>

                    {selectedDay && (
                        <Stack direction="row" spacing={2} mt={2}>
                            <TextField
                                label="Thời gian bắt đầu"
                                type="time"
                                value={startTime}
                                onChange={(e) => setStartTime(e.target.value)}
                                InputLabelProps={{ shrink: true }}
                                inputProps={{ step: 300 }} // 5 minutes
                            />
                            <TextField
                                label="Thời gian kết thúc"
                                type="time"
                                value={endTime}
                                onChange={(e) => setEndTime(e.target.value)}
                                InputLabelProps={{ shrink: true }}
                                inputProps={{ step: 300 }} // 5 minutes
                            />
                            <Button variant="contained" onClick={() => handleAddTimeSlot(selectedDay)}>
                                Lưu
                            </Button>
                        </Stack>
                    )}

                    <Typography variant='h6'>{daysOfWeek.find((day) => day.value === selectedDay)?.label}</Typography>
                    {selectedDay && schedule[selectedDay]?.length > 0 && (
                        <Grid container spacing={1} sx={{ mt: 1 }}>
                            {schedule[selectedDay].map((slot, index) => (
                                <Grid item xs={12} sm={6} md={4} key={index} sx={{ mb: 1 }}>
                                    <Box key={index} sx={{
                                        border: '1px solid lightgray',
                                        borderRadius: '4px',
                                        p: 1,
                                        textAlign: 'center',
                                        backgroundColor: '#f5f5f5',
                                    }}
                                        display={"flex"}
                                        alignItems={'center'}
                                        justifyContent={'center'}
                                        gap={1}>
                                        <Typography sx={{ mr: 2 }}>{`${slot.startTime} - ${slot.endTime}`}</Typography>
                                        <IconButton onClick={() => handleDeleteTimeSlot(selectedDay, index)} color="error">
                                            <DeleteIcon />
                                        </IconButton>
                                    </Box>
                                </Grid>
                            ))}

                        </Grid>

                    )}
                </Stack>
            </DialogContent>
            <DialogActions sx={{ padding: '20px', backgroundColor: '#f1f1f1' }}>
                <Button onClick={onClose} variant='contained' color="inherit">Huỷ</Button>
                <Button onClick={handleSave} variant='contained' color="primary">Lưu</Button>
            </DialogActions>
        </Dialog>
    );
}

export default CreateStudentRecordModal;
