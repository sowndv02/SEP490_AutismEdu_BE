import { Box, Button, Grid, Modal, TextField, Typography, Divider } from '@mui/material';
import React, { useState, useEffect } from 'react';
import ReactQuill from 'react-quill';
import { Formik, Field, Form } from 'formik';
import * as Yup from 'yup';

function CreateOrEditModal({ open, handleClose, handleSubmit, initialData, isEditing, tutorProfile }) {
    const [formData, setFormData] = useState({
        ageFrom: initialData?.ageFrom || '',
        ageEnd: initialData?.ageEnd || '',
        description: initialData?.description || '',
    });

    const [isDisabled, setIsDisabled] = useState(true);

    const validationSchema = Yup.object({
        ageFrom: Yup.number()
            .required('Độ tuổi bắt đầu là bắt buộc')
            .positive('Độ tuổi phải là số dương')
            .min(tutorProfile?.startAge, `Độ tuổi bắt đầu phải từ ${tutorProfile?.startAge} tuổi trở lên`)
            .max(14, 'Độ tuổi bắt đầu phải nhỏ hơn 14'),
        ageEnd: Yup.number()
            .required('Độ tuổi kết thúc là bắt buộc')
            .positive('Độ tuổi phải là số dương')
            .moreThan(Yup.ref('ageFrom'), 'Độ tuổi kết thúc phải lớn hơn độ tuổi bắt đầu')
            .max(tutorProfile?.endAge, `Độ tuổi kết thúc phải từ ${tutorProfile?.endAge} tuổi trở xuống`),
        description: Yup.string()
            .required('Nội dung chương trình học là bắt buộc')
            .test('is-not-empty', 'Không được để trống', value => value !== '<p><br></p>' && value !== '<p> </p>')
            .test(
                'max-length',
                'Không được vượt quá 2000 ký tự',
                (value) => {
                    const strippedContent = (value || '').replace(/<(.|\n)*?>/g, '').trim();
                    return strippedContent.length <= 2000;
                }
            )
            .test(
                'min-length',
                'Nội dung phải có ít nhất 5 ký tự',
                (value) => {
                    const strippedContent = (value || '').replace(/<(.|\n)*?>/g, '').trim();
                    return strippedContent.length >= 5;
                }
            ),
    });

    const style = {
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: 800,
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
        borderRadius: '10px',
    };

    useEffect(() => {
        const hasChanged =
            formData.ageFrom !== initialData?.ageFrom ||
            formData.ageEnd !== initialData?.ageEnd ||
            formData.description !== initialData?.description;

        setIsDisabled(!hasChanged);
    }, [formData, initialData]);

    const handleFormSubmit = (values) => {
        if (isEditing) {
            const data = initialData.originalCurriculum ? initialData.originalCurriculum.id : initialData.id;
            handleSubmit(values, data);
        } else {
            handleSubmit(values);
        }
        handleClose();
    };

    return (
        <Modal open={open} onClose={handleClose}>
            <Box sx={style}>
                <Typography textAlign={'center'} variant="h4" mb={2}>
                    {isEditing ? "Chỉnh sửa khung chương trình" : "Tạo khung chương trình"}
                </Typography>
                <Divider />
                <Formik
                    initialValues={formData}
                    validationSchema={validationSchema}
                    onSubmit={handleFormSubmit}
                    enableReinitialize
                >
                    {({ setFieldValue, values, errors, touched }) => (
                        <Form>
                            <Grid container spacing={2} mt={2}>
                                <Grid item xs={12}>
                                    <Typography variant="subtitle1" mb={1}>Độ tuổi của trẻ:</Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Field
                                        as={TextField}
                                        label="Từ"
                                        type="number"
                                        fullWidth
                                        name="ageFrom"
                                        value={values.ageFrom}
                                        error={touched.ageFrom && Boolean(errors.ageFrom)}
                                        helperText={touched.ageFrom && errors.ageFrom}
                                        onChange={(e) => {
                                            setFieldValue('ageFrom', e.target.value);
                                            setFormData((prev) => ({ ...prev, ageFrom: e.target.value }));
                                        }}
                                    />
                                </Grid>
                                <Grid item xs={6}>
                                    <Field
                                        as={TextField}
                                        label="Đến"
                                        type="number"
                                        fullWidth
                                        name="ageEnd"
                                        value={values.ageEnd}
                                        error={touched.ageEnd && Boolean(errors.ageEnd)}
                                        helperText={touched.ageEnd && errors.ageEnd}
                                        onChange={(e) => {
                                            setFieldValue('ageEnd', e.target.value);
                                            setFormData((prev) => ({ ...prev, ageEnd: e.target.value }));
                                        }}
                                    />
                                </Grid>
                                <Grid item xs={12} sx={{ height: '300px' }}>
                                    <Typography variant="subtitle1" mb={1}>Nội dung chương trình học:</Typography>
                                    <ReactQuill
                                        theme="snow"
                                        value={values.description}
                                        onChange={(content) => {
                                            setFieldValue('description', content);
                                            setFormData((prev) => ({ ...prev, description: content }));
                                        }}
                                    />
                                    {touched.description && errors.description ? (
                                        <Typography color="error" variant="body2" mt={1}>{errors.description}</Typography>
                                    ) : <Typography variant="body2" sx={{ mt: 1 }}>
                                        {values.description.replace(/<(.|\n)*?>/g, '').trim().length} / 2000
                                    </Typography>}
                                </Grid>
                            </Grid>
                            <Grid container spacing={2} justifyContent="center" mt={8} sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                                <Grid item>
                                    <Button variant="outlined" onClick={handleClose}>Hủy</Button>
                                </Grid>
                                <Grid item>
                                    <Button
                                        variant="contained"
                                        color="primary"
                                        type="submit"
                                        disabled={isEditing ? isDisabled : false}
                                    >
                                        {isEditing ? "Cập nhật" : "Tạo"}
                                    </Button>
                                </Grid>
                            </Grid>
                        </Form>
                    )}
                </Formik>
            </Box>
        </Modal>
    );
}

export default CreateOrEditModal;