import { Box, Button, FormHelperText, Grid, MenuItem, Select, TextField, Typography } from '@mui/material';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import ModalUploadAvatar from '~/pages/Tutor/TutorRegistration/TutorInformation/ModalUploadAvatar';
import axios from '~/plugins/axiosConfig';
import services from '~/plugins/services';
function ChildCreation({ handleClose, loading, setLoading, setModalOpen, open, setChild, handleGetChildInformation }) {
    const [avatar, setAvatar] = useState(null);
    useEffect(() => {
        if (!open) {
            setAvatar(null);
            formik.resetForm();
        }
    }, [open])
    const validate = values => {
        const errors = {};
        if (!values.fullName) {
            errors.fullName = "Bắt buộc"
        } else if (!/^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂÊÔưăêôƠƯÀẢÃÁẠĂẮẰẲẴẶÂẦẤẨẪẬÈẺẼÉẸÊỀẾỂỄỆÌỈĨÍỊÒỎÕÓỌÔỒỐỔỖỘƠỜỚỞỠỢÙỦŨÚỤƯỪỨỬỮỰỲỶỸÝỴàảãáạăắằẳẵặâầấẩẫậèẻẽéẹêềếểễệìỉĩíịòỏõóọôồốổỗộơờớởỡợùủũúụưừứửữựỳỷỹýỵ\s]+$/.test(values.fullName)) {
            errors.fullName = "Tên không hợp lệ!"
        } else if (values.fullName.length > 50) {
            errors.fullName = "Phải dưới 50 ký tự"
        }
        if (!values.gender) {
            errors.gender = "Bắt buộc"
        }
        if (!values.dateOfBirth) {
            errors.dateOfBirth = "Bắt buộc"
        }
        if (!avatar) {
            errors.avatar = "Bắt buộc"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            fullName: '',
            dateOfBirth: '',
            gender: 'True'
        },
        validate,
        onSubmit: async (values) => {
            try {
                setLoading(true);
                const formData = new FormData();
                formData.append("Name", values.fullName.trim());
                formData.append("isMale", values.gender);
                formData.append("BirthDate", values.dateOfBirth);
                formData.append("Media", avatar);
                axios.setHeaders({ "Content-Type": "multipart/form-data", "Accept": "application/json, text/plain, multipart/form-data, */*" });
                await services.ChildrenManagementAPI.createChild(formData, (res) => {
                    handleGetChildInformation();
                    setModalOpen(0);
                }, (err) => {
                    enqueueSnackbar(err.error[0], { variant: "error" })
                })
                axios.setHeaders({ "Content-Type": "application/json", "Accept": "application/json, text/plain, */*" });
                setLoading(false);
            } catch (error) {
                setLoading(false);
                enqueueSnackbar("Tạo thất bại!", { variant: "error" })
            }
        }
    });

    const getMaxDate = () => {
        const today = new Date();
        const lastYear = new Date(today);
        lastYear.setFullYear(today.getFullYear() - 1);
        const year = lastYear.getFullYear();
        const month = String(lastYear.getMonth() + 1).padStart(2, '0');
        const day = String(lastYear.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    const getMinDate = () => {
        const today = new Date();
        const fifteenYearsAgo = new Date(today);
        fifteenYearsAgo.setFullYear(today.getFullYear() - 15, 0, 1);
        const year = fifteenYearsAgo.getFullYear();
        const month = String(fifteenYearsAgo.getMonth() + 1).padStart(2, '0');
        const day = String(fifteenYearsAgo.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    return (
        <Dialog
            fullWidth="md"
            open={open}
            onClose={handleClose}
        >
            <DialogTitle>
                <Typography variant='h4'>
                    Thêm thông tin của trẻ
                </Typography>
            </DialogTitle>
            <form onSubmit={formik.handleSubmit}>
                <DialogContent>
                    <Grid container px="50px" py="50px" columnSpacing={2} rowSpacing={3}>
                        <Grid item xs={3} textAlign="right">Ảnh đại diện</Grid>
                        <Grid item xs={9}>
                            <ModalUploadAvatar setAvatar={setAvatar} />
                            {
                                !avatar && <FormHelperText error>
                                    Bắt buộc
                                </FormHelperText>
                            }
                            <Box>
                                {
                                    avatar &&
                                    <img src={URL.createObjectURL(avatar)} alt='avatar' width={150} />
                                }
                            </Box>
                        </Grid>
                        <Grid item xs={3} textAlign="right">Họ và tên</Grid>
                        <Grid item xs={9}>
                            <TextField size='small' fullWidth value={formik.values.fullName}
                                name='fullName'
                                onChange={formik.handleChange} />
                            {
                                formik.errors.fullName && (
                                    <FormHelperText error>
                                        {formik.errors.fullName}
                                    </FormHelperText>
                                )
                            }
                        </Grid>
                        <Grid item xs={3} textAlign="right">Giới tính</Grid>
                        <Grid item xs={9}>
                            <Select
                                name='gender'
                                value={formik.values.gender}
                                onChange={formik.handleChange}
                                fullWidth
                            >
                                <MenuItem value={"True"}>Nam</MenuItem>
                                <MenuItem value={"False"}>Nữ</MenuItem>
                            </Select>
                            {
                                formik.errors.gender && (
                                    <FormHelperText error>
                                        {formik.errors.gender}
                                    </FormHelperText>
                                )
                            }
                        </Grid>
                        <Grid item xs={3} textAlign="right">Ngày sinh</Grid>
                        <Grid item xs={9}>
                            <Box>
                                <TextField size='small' type='date' value={formik.values.dateOfBirth}
                                    name='dateOfBirth'
                                    onChange={formik.handleChange}
                                    inputProps={{
                                        max: getMaxDate(),
                                        min: getMinDate()
                                    }} />
                                {
                                    formik.errors.dateOfBirth && (
                                        <FormHelperText error>
                                            {formik.errors.dateOfBirth}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Typography variant='caption'>Chỉ tạo được trẻ từ 1 đến 15 tuổi</Typography>
                        </Grid>
                    </Grid>
                </DialogContent>
                <DialogActions>
                    <Button variant='contained' type='submit'>Thêm</Button>
                    <Button onClick={handleClose}>Huỷ</Button>
                </DialogActions>
            </form>
            <LoadingComponent open={loading} />
        </Dialog >
    )
}

export default ChildCreation
