import { Box, Button, Modal, Typography, Divider } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import React from 'react';
import services from '~/plugins/services';

function DeleteConfirmationModal({ id, open, handleClose, dataReviewStats, setDataReviewStats, handleGetDataReviewStats }) {
    console.log(id);
    const handleDelete = async () => {
        try {
            await services.ReviewManagementAPI.deleteReview(id, {}, async (res) => {
                if (res?.result) {
                    const newData = dataReviewStats?.reviews?.filter((r) => r?.id !== id);
                    setDataReviewStats((prev) => ({ ...prev, reviews: newData }));
                    enqueueSnackbar(res.result, { variant: 'success' });
                    await handleGetDataReviewStats();
                }
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        } finally {
            handleClose();
        }
    };

    const style = {
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: 500,
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
        borderRadius: '10px'
    };

    return (
        <Modal open={open} onClose={handleClose}>
            <Box sx={style}>
                <Typography textAlign={'center'} variant="h5" mb={2}>
                    Xác nhận xoá
                </Typography>
                <Divider />
                <Typography mt={2} mb={4}>
                    Bạn có chắc chắn muốn xoá đánh giá này không?
                </Typography>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
                    <Button variant="outlined" onClick={handleClose}>Huỷ</Button>
                    <Button variant="contained" color="primary" onClick={handleDelete}>Xoá</Button>
                </Box>
            </Box>
        </Modal>
    );
}

export default DeleteConfirmationModal;
