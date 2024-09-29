import React from 'react';
import { Box, Button, Modal, Typography, TextField, MenuItem, Select, FormControl, InputLabel, Grid, Divider } from '@mui/material';
import { Formik, Form } from 'formik';
import * as Yup from 'yup';
import ForwardToInboxIcon from '@mui/icons-material/ForwardToInbox';

function TutorRequestModal({ data = {
    child: '', // Field for the child selection
    phone: '0338581585', // Pre-filled phone number
    name: 'Nguyễn Văn A', // Pre-filled child name
    gender: 'Nam', // Pre-filled gender
    birthDate: '10-05-2008', // Pre-filled birth date
    note: '', // Field for notes to the tutor
} }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const style = {
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: 800,
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
    };

    // Validation schema for the form
    const validationSchema = Yup.object({
        child: Yup.string().required('Vui lòng chọn trẻ'),
        note: Yup.string().required('Vui lòng nhập ghi chú'),
    });

    return (
        <>
            <Button onClick={handleOpen} startIcon={<ForwardToInboxIcon />} variant='contained' color='primary' size='large'>
                Gửi yêu cầu
            </Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography textAlign={'center'} variant='h5' mb={2} id="modal-modal-title">Gửi yêu cầu cho gia sư</Typography>
                    <Divider />
                    <Formik
                        initialValues={data}
                        validationSchema={validationSchema}
                        onSubmit={(values) => {
                            console.log(values);
                            handleClose(); // Close the modal after submission
                        }}
                    >
                        {({ values, errors, touched, handleChange }) => (
                            <Form>
                                <Grid container spacing={2} mt={2}>
                                    {/* Chọn trẻ của bạn */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Chọn trẻ của bạn:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <FormControl fullWidth margin="dense">
                                                <InputLabel>Chọn trẻ của bạn</InputLabel>
                                                <Select
                                                    name="child"
                                                    value={values.child}
                                                    onChange={handleChange}
                                                    error={touched.child && Boolean(errors.child)}
                                                >
                                                    <MenuItem value="Nguyễn Văn A">Nguyễn Văn A</MenuItem>
                                                    <MenuItem value="Nguyễn Văn B">Nguyễn Văn B</MenuItem>
                                                </Select>
                                                {touched.child && errors.child && (
                                                    <Typography variant="body2" color="error">{errors.child}</Typography>
                                                )}
                                            </FormControl>
                                        </Grid>
                                    </Grid>

                                    {/* Số điện thoại */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Số điện thoại:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <Typography>{values.phone}</Typography>
                                        </Grid>
                                    </Grid>

                                    {/* Tên trẻ */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Tên trẻ:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <Typography>{values.name}</Typography>
                                        </Grid>
                                    </Grid>

                                    {/* Giới tính */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Giới tính:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <Typography>{values.gender}</Typography>
                                        </Grid>
                                    </Grid>

                                    {/* Ngày sinh */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Ngày sinh:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <Typography>{values.birthDate}</Typography>
                                        </Grid>
                                    </Grid>

                                    {/* Ghi chú tới gia sư */}
                                    <Grid item xs={12} container spacing={2} alignItems="center">
                                        <Grid item xs={4}>
                                            <Typography>Ghi chú tới gia sư:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <FormControl fullWidth margin="dense">
                                                <TextField
                                                    name="note"
                                                    label="Ghi chú tới gia sư"
                                                    multiline
                                                    rows={6}
                                                    value={values.note}
                                                    onChange={handleChange}
                                                    error={touched.note && Boolean(errors.note)}
                                                    helperText={touched.note && errors.note}
                                                />
                                            </FormControl>
                                        </Grid>
                                    </Grid>
                                </Grid>

                                <Box mt={3} display="flex" justifyContent="right">
                                    <Button variant="contained" color="inherit" onClick={handleClose} sx={{ mr: 2 }}>
                                        Hủy
                                    </Button>
                                    <Button type="submit" variant="contained" color="primary">
                                        Lưu
                                    </Button>
                                </Box>
                            </Form>
                        )}
                    </Formik>
                </Box>
            </Modal>
        </>
    );
}

export default TutorRequestModal;
