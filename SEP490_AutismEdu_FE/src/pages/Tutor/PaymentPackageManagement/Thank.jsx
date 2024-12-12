import React, { useEffect } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Typography, Box } from '@mui/material';

const Thank = ({ show, handleClose }) => {
    useEffect(() => {
        console.log(show);
    }, [show])
    return (
        <Dialog open={show} onClose={handleClose} maxWidth="xs" fullWidth>
            <DialogTitle variant='h5' textAlign={'center'}>
                Xin cảm ơn!
            </DialogTitle>
            <DialogContent dividers>
                <Box display="flex" flexDirection="column" alignItems="center" justifyContent="center">
                    <Box
                        component="img"
                        src={`https://cdn-icons-png.flaticon.com/512/5610/5610944.png`}
                        alt="Checkmark"
                        sx={{ width: '300px', marginBottom: 2 }}
                    />
                    <Typography variant="subtitle1" align="center" gutterBottom>
                        Bạn đã nâng cấp gói dịch vụ thành công.
                    </Typography>
                </Box>
            </DialogContent>
            <DialogActions>
                <Button variant="outlined" onClick={handleClose} color="primary">
                    Đóng
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default Thank;
