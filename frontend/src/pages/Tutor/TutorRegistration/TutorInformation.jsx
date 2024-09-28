import { Box, Button, Grid, MenuItem, Select, TextField } from '@mui/material'
import { useFormik } from 'formik';
import React from 'react'

function TutorInformation({ activeStep, handleBack, handleNext, steps }) {

    const validate = values => {
        const errors = {};
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            fullName: '',
            email: '',
            phoneNumber: '',
            birthDate: '',
            province: '',
            district: '',
            ward: '',
            fromAge: '',
            toAge: ''
        },
        validate,
        onSubmit: async (values) => {
            console.log(values);
        }
    });
    return (
        <>
            <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                <Grid item xs="4" textAlign="right">Họ và tên gia sư</Grid>
                <Grid item xs="8"> <TextField size='small' sx={{ width: "50%" }} /></Grid>
                <Grid item xs="4" textAlign="right">Email</Grid>
                <Grid item xs="8"> <TextField size='small' sx={{ width: "50%" }} /></Grid>
                <Grid item xs="4" textAlign="right">Số điện thoại</Grid>
                <Grid item xs="8"> <TextField size='small' sx={{ width: "50%" }} /></Grid>
                <Grid item xs="4" textAlign="right">Ngày sinh</Grid>
                <Grid item xs="8"> <TextField size='small' sx={{ width: "50%" }} type='date' /></Grid>
                <Grid item xs="4" textAlign="right">Địa chỉ</Grid>
                <Grid item xs="8">
                    <Select
                        labelId="demo-simple-select-label"
                        id="demo-simple-select"
                        value={formik.values}
                        onChange={formik.handleChange}
                        renderValue={(selected) => {
                            if (selected !== "") {
                                return <em>Tỉnh / TP</em>;
                            }
                        }}
                        size='small'
                    >
                        <MenuItem disabled value="">
                            <em>Tỉnh / TP</em>
                        </MenuItem>
                        <MenuItem value={20}>Twenty</MenuItem>
                        <MenuItem value={30}>Thirty</MenuItem>
                    </Select>
                    <Select
                        labelId="demo-simple-select-label"
                        id="demo-simple-select"
                        value={formik.values}
                        onChange={formik.handleChange}
                        renderValue={(selected) => {
                            if (selected !== "") {
                                return <em>Quận / Huyện</em>;
                            }
                        }}
                        size='small'
                        sx={{ ml: "20px" }}
                    >
                        <MenuItem disabled value="">
                            <em>Quận / Huyện</em>
                        </MenuItem>
                        <MenuItem value={20}>Twenty</MenuItem>
                        <MenuItem value={30}>Thirty</MenuItem>
                    </Select>
                    <Select
                        labelId="demo-simple-select-label"
                        id="demo-simple-select"
                        value={formik.values}
                        onChange={formik.handleChange}
                        renderValue={(selected) => {
                            if (selected !== "") {
                                return <em>Xã / Phường</em>;
                            }
                        }}
                        size='small'
                        sx={{ ml: "20px" }}
                    >
                        <MenuItem disabled value="">
                            <em>Xã / Phường</em>
                        </MenuItem>
                        <MenuItem value={20}>Twenty</MenuItem>
                        <MenuItem value={30}>Thirty</MenuItem>
                    </Select>
                    <Box mt="20px">
                        <TextField label="Số nhà, Thôn" size='small' />
                    </Box>
                </Grid>
                <Grid item xs="4" textAlign="right">Độ tuổi dạy</Grid>
                <Grid item xs="8">
                    <TextField size='small' label="Từ" sx={{ mr: "20px" }} type='number' />
                    <TextField size='small' label="Đến" type='number' />
                </Grid>
            </Grid>
            <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                <Button
                    color="inherit"
                    disabled={activeStep === 0}
                    onClick={handleBack}
                    sx={{ mr: 1 }}
                >
                    Back
                </Button>
                <Box sx={{ flex: '1 1 auto' }} />
                <Button onClick={handleNext}>
                    {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                </Button>
            </Box>
        </>
    )
}

export default TutorInformation
