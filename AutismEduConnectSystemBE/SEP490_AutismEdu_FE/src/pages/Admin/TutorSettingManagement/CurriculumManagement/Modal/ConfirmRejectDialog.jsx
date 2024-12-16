import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogActions, Button, DialogContent, TextField, Divider, Typography } from '@mui/material';

const ConfirmRejectDialog = ({ open, onClose, onConfirm }) => {
    const [reason, setReason] = useState('');
    const maxLength = 500;

    const handleConfirm = () => {
        onConfirm(reason?.trim());
        setReason('');
    };

    return (
        <Dialog open={open} onClose={onClose} fullWidth>
            <DialogTitle variant="h5" textAlign="center">Xác nhận từ chối</DialogTitle>
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
                    error={reason.trim().length > maxLength}
                    helperText={reason.trim().length > maxLength && "Không được vượt quá 500 ký tự!"}
                    sx={{ mt: 2 }}
                />
                <Typography 
                    variant="body2" 
                    color={reason.trim().length > maxLength ? 'error' : 'textSecondary'} 
                    sx={{ mt: 1, textAlign: 'right' }}
                >
                    {`${reason.trim().length} / ${maxLength} ký tự`}
                </Typography>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="inherit" variant="outlined">Hủy</Button>
                <Button 
                    onClick={handleConfirm} 
                    color="primary" 
                    variant="contained" 
                    disabled={!reason.trim() || reason.trim().length > maxLength}
                >
                    Xác nhận
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ConfirmRejectDialog;