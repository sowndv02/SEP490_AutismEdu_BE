import { Box, Button, Modal, Typography, Divider } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import React from 'react';
import services from '~/plugins/services';

function ConfirmShow({ open, handleClose, id, handleShowExerciseType }) {

    // const handleDelete = async () => {
    //     try {
    //         await services.CertificateAPI.deleteCertificate(id, {}, (res) => {
    //             const newListCerti = certificateList.filter((c) => c.id !== id);
    //             setCertificateList(newListCerti);
    //             enqueueSnackbar("Xoá thành công!", { variant: 'success' });
    //             handleClose();
    //         }, (error) => {
    //             console.log(error);
    //         })
    //     } catch (error) {
    //         console.log(error);
    //     }
    // }


    const style = {
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: 400,
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
        borderRadius: '10px'
    };

    return (
        <Modal open={open} onClose={handleClose}>
            <Box sx={style}>
                <Typography textAlign={'center'} variant="h5" mb={2}>
                    Xác nhận hiển thị loại bài tập
                </Typography>
                <Divider />
                <Typography mt={2} mb={4}>
                    Bạn có chắc chắn muốn hiển thị loại bài tập này không?
                </Typography>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
                    <Button variant="outlined" onClick={handleClose}>Huỷ</Button>
                    <Button variant="contained" color="primary" onClick={() => handleShowExerciseType(id)}>Hiện</Button>
                </Box>
            </Box>
        </Modal>
    );
}

export default ConfirmShow;
