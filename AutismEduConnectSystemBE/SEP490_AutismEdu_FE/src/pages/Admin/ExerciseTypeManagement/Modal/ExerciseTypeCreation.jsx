import React from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField, RadioGroup, FormControlLabel, Radio, Box, Divider } from '@mui/material';
import { useFormik } from 'formik';
import * as Yup from 'yup';

function ExerciseTypeCreation({ open, handleClose, handleCreateExerciseType }) {
    const formik = useFormik({
        initialValues: {
            exerciseTypeName: '',
            isHide: true,
        },
        validationSchema: Yup.object({
            exerciseTypeName: Yup.string().required('Tên loại bài tập không được để trống').max(100, 'Tên loại bài tập không quá 100 ký tự'),
        }),
        onSubmit: (values) => {
            handleCreateExerciseType(values);
            handleClose();
            formik.resetForm();
        },
    });

    return (
        <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
            <DialogTitle textAlign={'center'} variant='h5'>Thêm loại bài tập</DialogTitle>
            <Divider />
            <DialogContent>
                <form onSubmit={formik.handleSubmit}>
                    <Box mt={2} mb={2}>
                        <TextField
                            label="Tên loại bài tập"
                            fullWidth
                            name="exerciseTypeName"
                            value={formik.values.exerciseTypeName}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            error={formik.touched.exerciseTypeName && Boolean(formik.errors.exerciseTypeName)}
                            helperText={formik.touched.exerciseTypeName && formik.errors.exerciseTypeName}
                            variant="outlined"
                        />
                    </Box>
                    <Box mb={2}>
                        <RadioGroup
                            row
                            name="isHide"
                            value={formik.values.isHide}
                            onChange={(e) =>
                                formik.setFieldValue('isHide', e.target.value === 'true' ? true : false)
                            }
                        >
                            <FormControlLabel value={true} control={<Radio />} label="Ẩn" />
                            <FormControlLabel value={false} control={<Radio />} label="Hiện" />
                        </RadioGroup>
                    </Box>
                </form>
            </DialogContent>
            <DialogActions>
                <Button variant="outlined" onClick={handleClose}>
                    Huỷ
                </Button>
                <Button
                    variant="contained"
                    color="primary"
                    onClick={formik.handleSubmit}
                    disabled={!formik.isValid || formik.isSubmitting}
                >
                    Thêm
                </Button>
            </DialogActions>
        </Dialog>
    );
}

export default ExerciseTypeCreation;
