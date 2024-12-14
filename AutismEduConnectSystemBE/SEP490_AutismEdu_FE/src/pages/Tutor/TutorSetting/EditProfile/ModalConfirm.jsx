import React from 'react';
import {
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Button,
    Box
} from '@mui/material';

function ModalConfirm({ open, onClose, handleSubmit }) {
    return (
        <Dialog
            open={open}
            onClose={onClose}
            aria-labelledby="dialog-title"
            aria-describedby="dialog-description"
        >
            <DialogTitle id="dialog-title">Xác nhận lưu thay đổi</DialogTitle>
            <DialogContent>
                <DialogContentText id="dialog-description">
                    Bạn có chắc chắn muốn lưu các thay đổi này?
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button variant="outlined" color="inherit" onClick={onClose}>
                    Hủy
                </Button>
                <Button variant="contained" color="primary" onClick={handleSubmit}>
                    Đồng ý
                </Button>
            </DialogActions>
        </Dialog>
    );
}

export default ModalConfirm;