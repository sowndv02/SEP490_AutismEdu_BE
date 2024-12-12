import React, { useState } from 'react';
import {
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
} from '@mui/material';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';

const WorkExperienceCreation = ({ open, onClose, workExperienceList, setWorkExperienceList }) => {
    const [loading, setLoading] = useState(false);
    const getMinDate = () => {
        const currentDate = new Date();
        const pastDate = new Date();
        pastDate.setFullYear(currentDate.getFullYear() - 70);
        return pastDate.toISOString().slice(0, 7);
    }

    const getMaxDate = () => {
        const currentDate = new Date();
        const futureDate = new Date();
        futureDate.setFullYear(currentDate.getFullYear() + 70);
        return futureDate.toISOString().slice(0, 7)
    }

    const getCurrentDate = () => {
        const currentDate = new Date();
        return currentDate.toISOString().slice(0, 7)
    }
    const formik = useFormik({
        initialValues: {
            companyName: "",
            position: "",
            startDate: "",
            endDate: "",
            originalId: 0
        },
        validationSchema: Yup.object({
            companyName: Yup.string()
                .required("Tên công ty không được để trống").max(150, 'Tên công ty không được vượt quá 150 ký tự'),
            position: Yup.string()
                .required("Chức vụ không được để trống").max(100, 'Tên chức vụ không được vượt quá 100 ký tự'),
                startDate: Yup.date()
                .required("Không được để trống")
                .min(getMinDate(), `Thời gian bắt đầu phải sau ${getMinDate()?.split('-')?.reverse()?.join('-')}`)
                .max(getCurrentDate(), `Thời gian bắt đầu không được sau ${getCurrentDate()}`),
            endDate: Yup.date()
                .min(Yup.ref('startDate'), "Thời gian kết thúc phải sau thời gian bắt đầu")
                .max(getMaxDate(), `Thời gian kết thúc không được sau ${getMaxDate()?.split('-')?.reverse()?.join('-')}`)
                .typeError("Thời gian kết thúc không hợp lệ"),
        }),
        onSubmit: async (values, { resetForm }) => {
            try {
                setLoading(true);
                await services.WorkExperiencesAPI.createWorkExperience(values, (res) => {
                    if (res?.result) {
                        setWorkExperienceList([res.result, ...workExperienceList]);
                        enqueueSnackbar('Kinh nghiệm làm việc của bạn đã được thêm thành công!', { variant: 'success' });
                    }
                    onClose();
                    resetForm();
                }, (error => {
                    enqueueSnackbar(error.error[0], { variant: "error" });
                    console.log(error);
                }))
            } catch (error) {
                console.log(error);

            } finally {
                setLoading(false);
            }
        },
    });
    
    const handleClose = () => {
        formik.resetForm();
        onClose();
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
            <form onSubmit={formik.handleSubmit}>

                <DialogTitle textAlign={'center'} variant='h5'>Thêm kinh nghiệm làm việc</DialogTitle>
                <DialogContent>
                    <TextField
                        fullWidth
                        label="Tên công ty"
                        name="companyName"
                        value={formik.values.companyName}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        error={formik.touched.companyName && Boolean(formik.errors.companyName)}
                        helperText={formik.touched.companyName && formik.errors.companyName}
                        margin="normal"
                    />
                    <TextField
                        fullWidth
                        label="Chức vụ"
                        name="position"
                        value={formik.values.position}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        error={formik.touched.position && Boolean(formik.errors.position)}
                        helperText={formik.touched.position && formik.errors.position}
                        margin="normal"
                    />
                    <TextField
                        fullWidth
                        label="Thời gian làm việc bắt đầu"
                        name="startDate"
                        type="month"
                        value={formik.values.startDate}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        error={formik.touched.startDate && Boolean(formik.errors.startDate)}
                        helperText={formik.touched.startDate && formik.errors.startDate}
                        margin="normal"
                        InputLabelProps={{ shrink: true }}
                    />
                    <TextField
                        fullWidth
                        label="Đến"
                        name="endDate"
                        type="month"
                        value={formik.values.endDate}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        error={formik.touched.endDate && Boolean(formik.errors.endDate)}
                        helperText={formik.touched.endDate && formik.errors.endDate}
                        margin="normal"
                        InputLabelProps={{ shrink: true }}
                        disabled={formik.values.startDate === ""}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleClose} color="inherit" variant='outlined'>
                        Hủy
                    </Button>
                    <Button onClick={formik.handleSubmit} color="primary" variant="contained">
                        Lưu
                    </Button>
                </DialogActions>
            </form>

            <LoadingComponent open={loading} setOpen={setLoading} />
        </Dialog>
    );
};

export default WorkExperienceCreation;
