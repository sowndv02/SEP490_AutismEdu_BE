import { Box, Button, Modal, Typography, Divider } from '@mui/material';
import React from 'react';
import { useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';

function DoTestConfirmation({ open, handleClose, selectedTest }) {

    const nav = useNavigate();
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
                    Xác nhận làm
                </Typography>
                <Divider />
                <Typography mt={2} mb={4} variant='body1'>
                    Bạn có muốn làm bài kiểm tra <b>{selectedTest?.testName}</b> này không?
                </Typography>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
                    <Button variant="outlined" onClick={handleClose}>Huỷ</Button>
                    <Button variant="contained" color="primary" onClick={() => { nav(PAGES.ROOT + PAGES.DO_TEST, { state: { selectedTest } }) }}>Đồng ý</Button>
                </Box>
            </Box>
        </Modal>
    );
}

export default DoTestConfirmation;
