import React from 'react';
import { Dialog, DialogTitle, DialogContent, Typography, Box, Button, DialogActions, Divider, Grid, TextField } from '@mui/material';
import ReactQuill from 'react-quill';
import { NumericFormat } from 'react-number-format';
import PropTypes from 'prop-types';
const NumericFormatCustom = React.forwardRef(function NumericFormatCustom(props, ref) {
    const { onChange, ...other } = props;

    return (
        <NumericFormat
            {...other}
            getInputRef={ref}
            onValueChange={(values) => {
                onChange({
                    target: {
                        name: props.name,
                        value: values.value,
                    },
                });
            }}
            thousandSeparator="."
            decimalSeparator=","
            valueIsNumericString
        />
    );
});

NumericFormatCustom.propTypes = {
    name: PropTypes.string.isRequired,
    onChange: PropTypes.func.isRequired,
};

const UpdateRequestDetail = ({ open, onClose, request }) => {
    const formatAddress = (address) => {
        let adrs = address.split('|');
        return adrs.reverse().join(', ');
    };
    if (!request) return null;

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle variant='h4' textAlign={'center'}>Thông tin chi tiết</DialogTitle>
            <Divider />
            <DialogContent>
                <Grid container spacing={2}>

                    <Grid item xs={6} md={3}>
                        <TextField
                            fullWidth
                            label="Học phí từ"
                            variant="outlined"
                            name="priceFrom"
                            value={request.priceFrom || ''}
                            InputProps={{
                                inputComponent: NumericFormatCustom,
                            }}
                        />
                    </Grid>
                    <Grid item xs={6} md={3}>
                        <TextField
                            fullWidth
                            label="Đến"
                            variant="outlined"
                            name="priceEnd"
                            value={request.priceEnd || ''}
                            InputProps={{
                                inputComponent: NumericFormatCustom,
                            }}
                        />
                    </Grid>
                    <Grid item xs={12} md={6}>
                        <TextField
                            fullWidth
                            aria-readonly
                            label="Email"
                            variant="outlined"
                            name="email"
                            value={request?.tutor?.email || ''}
                        />
                    </Grid>
                    <Grid item xs={4} md={2}>
                        <TextField
                            aria-readonly
                            fullWidth
                            label="Số giờ dạy trên buổi"
                            variant="outlined"
                            name="sessionHours"
                            value={request?.sessionHours || ''}
                        />
                    </Grid>

                    <Grid item xs={4} md={2}>
                        <TextField
                            aria-readonly
                            type='number'
                            fullWidth
                            label="Tuổi từ"
                            variant="outlined"
                            name="startAge"
                            value={request?.startAge || ''}
                        />
                    </Grid>
                    <Grid item xs={4} md={2}>
                        <TextField
                            aria-readonly
                            type='number'
                            fullWidth
                            label="Đến"
                            variant="outlined"
                            name="endAge"
                            value={request?.endAge || ''}
                        />
                    </Grid>
                    <Grid item xs={12} md={6}>
                        <TextField
                            fullWidth
                            label="Số điện thoại"
                            variant="outlined"
                            name="phoneNumber"
                            value={request?.phoneNumber || ''}
                        />
                    </Grid>
                    <Grid item xs={12} md={6}>
                        <TextField
                            aria-readonly
                            fullWidth
                            label="Địa chỉ"
                            variant="outlined"
                            name="address"
                            value={formatAddress(request?.address || '')}
                        />
                    </Grid>

                    <Grid item xs={12} md={6}>
                        <TextField
                            aria-readonly
                            fullWidth
                            label="Ngày tạo"
                            variant="outlined"
                            name="createdDate"
                            value={new Date(request?.createdDate).toLocaleDateString() || ''}
                        />
                    </Grid>

                    <Grid item xs={12} mb={0} sx={{ height: '350px' }}>
                        <Typography variant='h6' mb={2}>Giới thiệu về tôi</Typography>
                        <ReactQuill
                            readOnly
                            value={request?.aboutMe || ''}
                        />
                    </Grid>
                    {request?.rejectionReason &&
                        <Grid item xs={12} md={12}>
                            <TextField
                                readOnly
                                fullWidth
                                id="outlined-multiline-static"
                                label="Lý do từ chối"
                                multiline
                                color='error'
                                focused
                                rows={2}
                                value={request?.rejectionReason}
                            />
                        </Grid>
                    }

                </Grid>

            </DialogContent>
            <DialogActions>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <Button onClick={onClose} color="primary" variant="contained">
                        Đóng
                    </Button>
                </Box>
            </DialogActions>
        </Dialog>
    );
};

export default UpdateRequestDetail;