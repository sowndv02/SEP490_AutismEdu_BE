import { Box, Button, Modal, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';

function ReportReview({ open, setOpen, currentReport }) {
    const [loading, setLoading] = useState(false);
    const [description, setDescription] = useState("");
    const handleSubmit = async () => {
        try {
            setLoading(true);
            if (!description) {
                enqueueSnackbar("Nhập lý do tố cáo!", { variant: "error" });
                return;
            }
            await services.ReportManagementAPI.createReviewReport({
                reviewId: currentReport.id,
                description: description
            }, (res) => {
                enqueueSnackbar("Tố cáo thành công", { variant: "success" });
                setOpen(false);
                setDescription("");
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
            })
        } catch (error) {
            enqueueSnackbar("Tố cáo thất bại", { variant: "error" });
        } finally {
            setLoading(false);
        }
    }
    return (
        <Modal
            open={open}
            onClose={() => setOpen(false)}
        >
            <Box sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                width: 500,
                bgcolor: 'background.paper',
                boxShadow: 24,
                p: 4
            }}>
                <Typography variant="h5" component="h2">
                    Tố cáo đánh giá
                </Typography>
                <Typography sx={{ mt: 2 }}>
                    <TextField multiline rows={5}
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder='Nhập lý do tố cáo' fullWidth />
                </Typography>
                <Box mt={3} textAlign="end">
                    <Button onClick={() => setOpen(false)} variant='outlined' color='inherit'>Huỷ</Button>
                    <Button variant='contained' color='error' onClick={handleSubmit} sx={{ml:2}}>Gửi</Button>
                </Box>
                <LoadingComponent open={loading} />
            </Box>
        </Modal>
    )
}

export default ReportReview
