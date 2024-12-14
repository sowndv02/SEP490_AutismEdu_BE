import React from 'react';
import {
    Typography,
    Grid,
    Button,
    Dialog,
    DialogContent,
    DialogActions,
    Divider,
    DialogTitle,
    Box,
} from '@mui/material';
import { format } from 'date-fns';

const PaymentHistoryDetail = ({ open, onClose, paymentHistory }) => {
    if (!paymentHistory) {
        return null;
    }

    return (
        <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
            <DialogTitle variant="h5" textAlign="center">
                Chi tiết lịch sử thanh toán
            </DialogTitle>
            <Divider />
            <DialogContent sx={{ py: 3 }}>
                <Box sx={{ width: '80%', mx: 'auto' }}>
                    <Grid container spacing={3}>
                        {[
                            { label: 'Email', value: paymentHistory?.submitter?.email },
                            { label: 'Họ và tên', value: paymentHistory?.submitter?.fullName },
                            { label: 'Mã giao dịch', value: paymentHistory.transactionId },
                            {
                                label: 'Số tiền',
                                value: paymentHistory.amount
                                    ? `${paymentHistory.amount.toLocaleString('vi-VN')} đ`
                                    : null,
                            },
                            {
                                label: 'Thời gian thanh toán',
                                value: paymentHistory?.paymentDate
                                    ? format(new Date(paymentHistory.paymentDate), 'HH:mm dd/MM/yyyy')
                                    : null,
                            },
                            { label: 'Mã giao dịch ngân hàng', value: paymentHistory.bankTransactionId },
                            { label: 'Gói thanh toán', value: paymentHistory.packagePayment?.title },
                            {
                                label: 'Mô tả về gói thanh toán',
                                value: paymentHistory.packagePayment?.description,
                            },
                            {
                                label: 'Ngày hết hạn',
                                value: paymentHistory.expirationDate
                                    ? new Date(paymentHistory.expirationDate).toLocaleDateString()
                                    : null,
                            },
                            { label: 'Mô tả', value: paymentHistory?.description },
                        ].map(({ label, value }, index) => (
                            <React.Fragment key={index}>
                                <Grid item xs={5}>
                                    <Typography
                                        variant='subtitle1'
                                    >
                                        {label}:
                                    </Typography>
                                </Grid>
                                <Grid item xs={7}>
                                    <Typography
                                        variant='subtitle1'
                                    >
                                        {value || 'Không có'}
                                    </Typography>
                                </Grid>
                            </React.Fragment>
                        ))}
                    </Grid>
                </Box>
            </DialogContent>
            <Divider />
            <DialogActions sx={{ justifyContent: 'right', py: 2 }}>
                <Button onClick={onClose} color="primary" variant="contained">
                    Đóng
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default PaymentHistoryDetail;
