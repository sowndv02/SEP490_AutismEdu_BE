import React, { useState } from 'react';
import DeleteIcon from '@mui/icons-material/Delete';
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import axios from "~/plugins/axiosConfig";
import {
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    IconButton,
    TextField,
    Typography,
    Grid,
    Divider
} from '@mui/material';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import services from '~/plugins/services';

export default function CreateCertificateDialog({ open, onClose, certificateData, setCertificateData, certificateList, setCertificateList }) {
    const [selectedImage, setSelectedImage] = useState(null);
    const [loading, setLoading] = useState(false);
    const [openImageDialog, setOpenImageDialog] = useState(false);
    const [listImg, setListImg] = useState([]);
    const handleImageClick = (image) => {
        setSelectedImage(image);
        setOpenImageDialog(true);
    };

    const handleCloseImageDialog = () => {
        setOpenImageDialog(false);
        setSelectedImage(null);
    };

    const handleSubmitCertificate = async () => {
        try {
            setLoading(true);
            const formData = new FormData();

            formData.append('CertificateName', certificateData.CertificateName);
            formData.append('IssuingInstitution', certificateData.IssuingInstitution);
            formData.append('IdentityCardNumber', certificateData.IdentityCardNumber);
            formData.append('IssuingDate', certificateData.IssuingDate);
            formData.append('ExpirationDate', certificateData.ExpirationDate);

            certificateData.Medias.forEach((file, index) => {
                formData.append(`Medias`, file);
            });

            axios.setHeaders({ "Content-Type": "multipart/form-data", "Accept": "application/json, text/plain, multipart/form-data, */*" });
            await services.CertificateAPI.createCertificate(formData, (res) => {
                setCertificateList([res.result, ...certificateList]);
                enqueueSnackbar('Chứng chỉ của bạn đã được tạo thành công!', { variant: 'success' })
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: "error" });
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
            setCertificateData({
                CertificateName: '',
                IssuingInstitution: '',
                IdentityCardNumber: '',
                IssuingDate: '',
                ExpirationDate: '',
                Medias: []
            });
        }
        axios.setHeaders({ "Content-Type": "application/json", "Accept": "application/json, text/plain, */*" });
    };

    const handleImageRemove = (idx) => {
        const updatedImages = listImg.filter((_, index) => index !== idx);
        setListImg(updatedImages);
        const updatedMedias = certificateData.Medias.filter((_, index) => index !== idx);
        setCertificateData({ ...certificateData, Medias: updatedMedias });
        formik.setFieldValue('Medias', updatedImages);
    };

    const formik = useFormik({
        initialValues: {
            CertificateName: certificateData.CertificateName,
            IssuingInstitution: certificateData.IssuingInstitution,
            IdentityCardNumber: certificateData.IdentityCardNumber,
            IssuingDate: certificateData.IssuingDate,
            ExpirationDate: certificateData.ExpirationDate,
            Medias: certificateData.Medias,
        },
        validationSchema: Yup.object({
            CertificateName: Yup.string().required('Tên chứng chỉ là bắt buộc').max(150, 'Tên chứng chỉ không được vượt quá 150 ký tự'),
            IssuingInstitution: Yup.string().required('Nơi cấp là bắt buộc').max(100, 'Nơi cấp không được vượt quá 100 ký tự'),
            IssuingDate: Yup.date().required('Ngày cấp là bắt buộc'),
            ExpirationDate: Yup.date().nullable(),
            Medias: Yup.array()
                .min(1, 'Phải có ít nhất một ảnh')
                .max(5, 'Không được tải lên quá 5 ảnh'),
        }),
        onSubmit: async (values) => {
            await handleSubmitCertificate();
            onClose();
        },
    });


    const handleInputChange = (event) => {
        const { name, value } = event.target;
        const text = ['CertificateName', 'IssuingInstitution'];

        const updatedValue = text.includes(name) ? value.trim() : value;

        setCertificateData({
            ...certificateData,
            [name]: updatedValue,
        });

        formik.setFieldValue(name, updatedValue);
    };

    const handleImageUploadWrapper = (event) => {
        const files = event.target.files;
        const fileArray = Array.from(files);
        const uploadedImages = fileArray.map((file) => {
            return { url: URL.createObjectURL(file) };
        });
        setListImg(uploadedImages);
        setCertificateData({ ...certificateData, Medias: fileArray });
        formik.setFieldValue('Medias', uploadedImages);
    };

    const getMinDate = () => {
        const today = new Date();
        const fifteenYearsAgo = new Date(today);
        fifteenYearsAgo.setFullYear(today.getFullYear() - 70, 0, 1);
        const year = fifteenYearsAgo.getFullYear();
        const month = String(fifteenYearsAgo.getMonth() + 1).padStart(2, '0');
        const day = String(fifteenYearsAgo.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    const getMaxDate = () => {
        const today = new Date();
        const lastYear = new Date(today);
        lastYear.setFullYear(today.getFullYear() + 70);
        const year = lastYear.getFullYear();
        const month = String(lastYear.getMonth() + 1).padStart(2, '0');
        const day = String(lastYear.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <form onSubmit={formik.handleSubmit}>
                <DialogTitle sx={{ textAlign: 'center' }} variant='h5'>Tạo chứng chỉ</DialogTitle>
                <Divider />
                <DialogContent>
                    <Grid container spacing={2} mb={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ mt: 1, fontWeight: '500', textAlign: 'right' }}>Tên chứng chỉ:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                fullWidth
                                name="CertificateName"
                                value={certificateData.CertificateName}
                                onChange={handleInputChange}
                                error={formik.touched.CertificateName && Boolean(formik.errors.CertificateName)}
                                helperText={formik.touched.CertificateName && formik.errors.CertificateName}
                                variant="outlined"
                                size="small"
                            />
                        </Grid>
                    </Grid>
                    <Grid container spacing={2} mb={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ mt: 1, fontWeight: '500', textAlign: 'right' }}>Nơi cấp:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                fullWidth
                                name="IssuingInstitution"
                                value={certificateData.IssuingInstitution}
                                onChange={handleInputChange}
                                error={formik.touched.IssuingInstitution && Boolean(formik.errors.IssuingInstitution)}
                                helperText={formik.touched.IssuingInstitution && formik.errors.IssuingInstitution}
                                variant="outlined"
                                size="small"
                            />
                        </Grid>
                    </Grid>

                    <Grid container spacing={2} mb={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ mt: 1, fontWeight: '500', textAlign: 'right' }}>Ngày cấp:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                fullWidth
                                type="date"
                                name="IssuingDate"
                                value={certificateData.IssuingDate}
                                onChange={handleInputChange}
                                error={formik.touched.IssuingDate && Boolean(formik.errors.IssuingDate)}
                                helperText={formik.touched.IssuingDate && formik.errors.IssuingDate}
                                variant="outlined"
                                size="small"
                                inputProps={{
                                    min: getMinDate(),
                                    max: new Date().toISOString().split('T')[0]
                                }}
                            />
                        </Grid>
                    </Grid>
                    <Grid container spacing={2} mb={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ mt: 1, fontWeight: '500', textAlign: 'right' }}>Ngày hết hạn (Không bắt buộc):</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                fullWidth
                                type="date"
                                name="ExpirationDate"
                                value={certificateData.ExpirationDate}
                                onChange={handleInputChange}
                                error={formik.touched.ExpirationDate && Boolean(formik.errors.ExpirationDate)}
                                helperText={formik.touched.ExpirationDate && formik.errors.ExpirationDate}
                                variant="outlined"
                                size="small"
                                disabled={!certificateData.IssuingDate}
                                inputProps={{
                                    min: certificateData.IssuingDate,
                                    max: getMaxDate()
                                }}
                            />
                        </Grid>
                    </Grid>
                    <Grid container spacing={2} alignItems="center" mb={2}>
                        <Grid item xs={4}>
                            <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Hình ảnh chứng chỉ:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <Button
                                variant="outlined"
                                component="label"
                                startIcon={<PhotoCameraIcon />}
                                sx={{ padding: '6px 16px', fontWeight: '500' }}
                            >
                                Tải lên hình ảnh
                                <input
                                    hidden
                                    accept="image/*"
                                    multiple
                                    type="file"
                                    onChange={handleImageUploadWrapper}
                                />
                            </Button>
                            {formik.errors.Medias && formik.touched.Medias && (
                                <Typography variant="body2" color="error" sx={{ marginTop: 1 }}>
                                    {formik.errors.Medias}
                                </Typography>
                            )}
                        </Grid>
                    </Grid>
                    <Grid container>
                        <Grid item xs={4}></Grid>
                        <Grid item xs={8}>
                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                                {listImg?.map((image, index) => (
                                    <Box key={index} sx={{ position: 'relative', width: 100, height: 100, borderRadius: '8px', overflow: 'hidden', boxShadow: '0px 2px 10px rgba(0, 0, 0, 0.1)' }}>
                                        <img src={image.url} alt="Chứng chỉ" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                        <IconButton
                                            sx={{ position: 'absolute', top: 5, right: 5, color: 'red', backgroundColor: 'rgba(0, 0, 0, 0.5)' }}
                                            size="small"
                                            onClick={() => handleImageRemove(index)}
                                        >
                                            <DeleteIcon fontSize="small" />
                                        </IconButton>
                                        <IconButton
                                            sx={{ position: 'absolute', top: 5, left: 5, color: '#fff', backgroundColor: 'rgba(0, 0, 0, 0.5)' }}
                                            size="small"
                                            onClick={() => handleImageClick(image.url)}
                                        >
                                            <VisibilityIcon fontSize="small" />
                                        </IconButton>
                                    </Box>
                                ))}
                            </Box>
                        </Grid>
                    </Grid>
                </DialogContent>
                <DialogActions>
                    <Button onClick={onClose} color="inherit" variant='outlined'>Hủy</Button>
                    <Button type="submit" color="primary" variant='contained'>Lưu</Button>
                </DialogActions>
            </form>
            <Dialog open={openImageDialog} onClose={handleCloseImageDialog} maxWidth="md" fullWidth>
                <DialogContent>
                    {selectedImage && (
                        <Box sx={{ textAlign: 'center' }}>
                            <img src={selectedImage} alt="Chứng chỉ" style={{ width: '100%', height: 'auto', maxHeight: '500px', objectFit: 'contain' }} />
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseImageDialog} color="inherit" variant='outlined'>Đóng</Button>
                </DialogActions>
            </Dialog>
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Dialog>
    );
}
