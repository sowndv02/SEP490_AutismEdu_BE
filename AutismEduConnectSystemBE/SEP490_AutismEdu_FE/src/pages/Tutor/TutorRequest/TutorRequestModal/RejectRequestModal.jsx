import React from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField, Typography, Grid, Select, MenuItem, FormControl, InputLabel } from '@mui/material';
import { Formik, Form } from 'formik';
import * as Yup from 'yup';

const RejectRequestModal = ({ open, onClose, onConfirm }) => {
    const validationSchema = Yup.object().shape({
        reason: Yup.string()
            .required('Lý do không được để trống')
            .min(5, 'Lý do phải có ít nhất 5 ký tự')
            .max(500, 'Không được nhập quá 500 ký tự'),
        rejectType: Yup.string().required('Loại từ chối không được để trống'),
    });

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle sx={{ backgroundColor: '#556cd6', color: '#fff', textAlign: 'center', fontWeight: 'bold' }}>
                Xác nhận từ chối
            </DialogTitle>
            <DialogContent>
                <Formik
                    initialValues={{ rejectType: 1, reason: '' }}
                    validationSchema={validationSchema}
                    onSubmit={(values) => {
                        onConfirm(values);
                        onClose();
                    }}
                >
                    {({ values, handleChange, handleBlur, errors, touched }) => (
                        <Form>
                            <Grid container spacing={3} mt={5}>
                                <Grid item xs={4}>
                                    <Typography variant="body1" sx={{ fontWeight: 600, mb: 2 }}>
                                        Loại từ chối:
                                    </Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <FormControl fullWidth error={touched.rejectType && Boolean(errors.rejectType)}>
                                        <InputLabel>Chọn loại từ chối</InputLabel>
                                        <Select
                                            name="rejectType"
                                            value={values.rejectType}
                                            onChange={handleChange}
                                            onBlur={handleBlur}
                                            label="Chọn loại từ chối"
                                        >
                                            <MenuItem value={1}>Không tương thích với chương trình giảng dạy</MenuItem>
                                            <MenuItem value={2}>Xung đột lịch trình</MenuItem>
                                            <MenuItem value={3}>Lý do khác</MenuItem>
                                        </Select>
                                        {touched.rejectType && errors.rejectType && (
                                            <Typography variant="caption" color="error">
                                                {errors.rejectType}
                                            </Typography>
                                        )}
                                    </FormControl>
                                </Grid>

                                <Grid item xs={4}>
                                    <Typography variant="body1" sx={{ fontWeight: 600, mb: 2 }}>
                                        Lý do:
                                    </Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <TextField
                                        fullWidth
                                        multiline
                                        rows={4}
                                        variant="outlined"
                                        name="reason"
                                        value={values.reason}
                                        onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder="Nhập lý do từ chối"
                                        error={touched.reason && Boolean(errors.reason)}
                                        helperText={touched.reason && errors.reason}
                                    />
                                </Grid>
                            </Grid>
                            <DialogActions>
                                <Button onClick={onClose} color="inherit" variant="outlined">
                                    Huỷ
                                </Button>
                                <Button
                                    type="submit"
                                    color="primary"
                                    variant="contained"
                                    disabled={!values.reason || !!errors.reason || !values.rejectType || !!errors.rejectType}
                                >
                                    Xác nhận
                                </Button>
                            </DialogActions>
                        </Form>
                    )}
                </Formik>
            </DialogContent>
        </Dialog>
    );
};

export default RejectRequestModal;
