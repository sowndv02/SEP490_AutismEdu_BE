import React from 'react';
import { Dialog, DialogTitle, DialogActions, Button, DialogContent, Typography, Divider } from '@mui/material';

const ConfirmAcceptDialog = ({ open, onClose, onConfirm }) => {
    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle variant='h5' textAlign={'center'}>Xác nhận chấp nhận</DialogTitle>
            <Divider/>
            <DialogContent>
                <Typography>Bạn có chắc chắn muốn chấp nhận yêu cầu này?</Typography>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="inherit" variant='outlined'>Hủy</Button>
                <Button onClick={onConfirm} color="primary" variant='contained'>Xác nhận</Button>
            </DialogActions>
        </Dialog>
    );
};

export default ConfirmAcceptDialog;
