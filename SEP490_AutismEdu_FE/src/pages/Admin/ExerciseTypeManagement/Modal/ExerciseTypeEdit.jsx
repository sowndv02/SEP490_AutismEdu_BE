import React from 'react';
import { Dialog, DialogActions, DialogContent, DialogTitle, TextField, Button, Divider } from '@mui/material';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { enqueueSnackbar } from 'notistack';

const ExerciseTypeEdit = ({ open, onClose, exerciseTypeList, setExerciseTypeList, eType }) => {
    const [loading, setLoading] = React.useState(false);

    // Formik configuration
    const formik = useFormik({
        initialValues: {
            exerciseTypeName: eType.exerciseTypeName || '', 
        },
        validationSchema: Yup.object({
            exerciseTypeName: Yup.string()
                .required('Tên loại bài tập không được để trống')
                .max(100, 'Tên loại bài tập không quá 100 ký tự'),
        }),
        onSubmit: async (values) => {
            try {
                setLoading(true);
                const newData = {
                    id: eType?.id,
                    exerciseTypeName: values.exerciseTypeName?.trim(),
                };
                await services.ExerciseManagementAPI.updateExerciseType(eType?.id, newData, (res) => {
                    if (res?.result) {
                        const newExerciseType = exerciseTypeList?.find((e) => e?.id === eType?.id);
                        newExerciseType.exerciseTypeName = res.result.exerciseTypeName;
                        setExerciseTypeList([...exerciseTypeList]); 
                        enqueueSnackbar('Cập nhật loại bài tập thành công!', { variant: 'success' });
                    }
                }, (error) => {
                    enqueueSnackbar('Chưa nhập tên loại bài tập', { variant: 'error' });
                    console.log(error);
                });
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false);
                onClose();
            }
        },
    });

    return (
        <Dialog open={open} onClose={onClose} fullWidth>
            <form onSubmit={formik.handleSubmit}>
                <DialogTitle variant='h5' textAlign={'center'}>Chỉnh sửa loại bài tập</DialogTitle>
                <Divider />
                <DialogContent>
                    <TextField
                        margin='dense'
                        label="Tên loại bài tập"
                        variant="outlined"
                        fullWidth
                        size='medium'
                        id="exerciseTypeName"
                        name="exerciseTypeName"
                        value={formik.values.exerciseTypeName}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        error={formik.touched.exerciseTypeName && Boolean(formik.errors.exerciseTypeName)}
                        helperText={formik.touched.exerciseTypeName && formik.errors.exerciseTypeName}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={onClose} color="inherit" variant='outlined'>
                        Hủy
                    </Button>
                    <Button type="submit" color="primary" variant='contained' disabled={formik.values.exerciseTypeName === eType?.exerciseTypeName}>
                        Lưu
                    </Button>
                </DialogActions>
            </form>
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Dialog>
    );
};

export default ExerciseTypeEdit;
