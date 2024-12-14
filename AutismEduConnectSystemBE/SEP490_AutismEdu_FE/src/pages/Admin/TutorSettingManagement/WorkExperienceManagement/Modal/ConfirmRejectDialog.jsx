import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogActions, Button, DialogContent, TextField, Divider } from '@mui/material';

const ConfirmRejectDialog = ({ open, onClose, onConfirm }) => {
    const [reason, setReason] = useState('');

    const handleConfirm = () => {
        onConfirm(reason);
        setReason('');
    };

    return (
        <Dialog open={open} onClose={onClose} fullWidth>
            <DialogTitle variant='h5' textAlign={'center'}>Xác nhận từ chối</DialogTitle>
            <Divider />
            <DialogContent>
                <TextField
                    required
                    fullWidth
                    label="Lý do từ chối"
                    multiline
                    rows={3}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    sx={{ mt: 2 }}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="inherit" variant='outlined'>Hủy</Button>
                <Button onClick={handleConfirm} color="primary" variant='contained' disabled={!reason.trim()}>Xác nhận</Button>
            </DialogActions>
        </Dialog>
    );
};

export default ConfirmRejectDialog;
