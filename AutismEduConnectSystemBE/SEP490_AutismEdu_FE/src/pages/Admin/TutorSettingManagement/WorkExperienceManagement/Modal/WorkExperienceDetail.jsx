import React from 'react';
import {
    Typography,
    Grid,
    Button,
    Dialog,
    DialogContent,
    DialogActions,
    TextField,
    Box,
    DialogTitle,
    Divider,
} from '@mui/material';

const WorkExperienceDetail = ({ open, onClose, workExperience }) => {
    const statusText = (status) => {
        switch (status) {
            case 0:
                return 'Từ chối';
            case 1:
                return 'Chấp nhận';
            case 2:
                return 'Chờ duyệt';
            default:
                return 'Không rõ';
        }
    };

    if (!workExperience) {
        return null;
    }

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle variant='h5' textAlign={'center'}>
                Chi tiết kinh nghiệm làm việc
            </DialogTitle>
            <Divider />
            <DialogContent>
                <Box sx={{ width: '80%', mx: 'auto' }}>
                    <Grid container spacing={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Email:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {(workExperience?.submitter?.email ?? workExperience?.tutorRegistrationRequest?.email) || 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Họ và tên:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {(workExperience?.submitter?.fullName ?? workExperience?.tutorRegistrationRequest?.fullName) || 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Tên cty/doanh nghiệp:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {workExperience.companyName || 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Vị trí:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {workExperience.position || 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Thời gian bắt đầu:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {workExperience.startDate
                                    ? new Date(workExperience.startDate).toLocaleDateString()
                                    : 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Thời gian kết thúc:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Typography>
                                {workExperience.endDate
                                    ? new Date(workExperience.endDate).toLocaleDateString()
                                    : 'Không có'}
                            </Typography>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Trạng thái:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Button
                                variant="outlined"
                                color={
                                    workExperience.requestStatus === 1
                                        ? 'success'
                                        : workExperience.requestStatus === 0
                                            ? 'error'
                                            : 'warning'
                                }
                                size="medium"
                                sx={{ borderRadius: 2, textTransform: 'none' }}
                            >
                                {statusText(workExperience?.requestStatus)}
                            </Button>
                        </Grid>

                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: 'bold', textAlign: 'right' }}>Lý do từ chối:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                value={workExperience?.rejectionReason || 'Không có phản hồi'}
                                fullWidth
                                multiline
                                readOnly
                                rows={4}
                                variant="outlined"
                                sx={{ backgroundColor: '#f0f0f0', borderRadius: 1 }}
                            />
                        </Grid>
                    </Grid>
                </Box>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="primary" variant="contained">
                    Đóng
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default WorkExperienceDetail;
