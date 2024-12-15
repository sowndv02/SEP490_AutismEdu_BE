import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Typography, Paper, Grid, Button, Divider, TextField, Dialog, DialogContent, DialogActions, IconButton, Stack } from '@mui/material';
import services from '~/plugins/services';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { formatDate } from 'date-fns';

function CertificateDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [certificate, setCertificate] = useState(null);
    const [selectedImage, setSelectedImage] = useState(null);
    const [openImageDialog, setOpenImageDialog] = useState(false);

    const handleImageClick = (image) => {
        setSelectedImage(image);
        setOpenImageDialog(true);
    };

    const handleCloseImageDialog = () => {
        setOpenImageDialog(false);
        setSelectedImage(null);
    };

    useEffect(() => {
        fetchCertificateDetail();
    }, [id]);

    const fetchCertificateDetail = async () => {
        try {
            await services.CertificateAPI.getCertificateById(id, (res) => {
                setCertificate(res.result);
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }
    };

    const statusText = (status) => {
        switch (status) {
            case 0:
                return 'Từ chối';
            case 1:
                return 'Chấp nhận';
            case 2:
                return 'Chờ duyệt';
            default:
                return 'Không rõ';
        }
    };

    if (!certificate) {
        return <Typography>Loading...</Typography>;
    }

    return (
        <Box sx={{ width: '100%', maxWidth: '800px', margin: 'auto', mt: 4, px: 2 }}>
            <Paper
                sx={{
                    p: 4,
                    boxShadow: 3,
                    borderRadius: 2,
                    backgroundColor: '#f9f9f9',
                    border: '1px solid #ddd',
                }}
            >
                {!certificate?.identityCardNumber ? <Typography variant="h4" sx={{ mb: 3, fontWeight: 'bold', textAlign: 'center' }}>
                    Chi tiết chứng chỉ
                </Typography> : <Typography variant="h4" sx={{ mb: 3, fontWeight: 'bold', textAlign: 'center' }}>
                    Chi tiết căn cước công dân
                </Typography>}
                <Grid container spacing={2} alignItems="center">
                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Tên {!certificate?.identityCardNumber ? 'chứng chỉ' : ''}:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>{certificate.certificateName}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Nơi cấp:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>{certificate.issuingInstitution}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Ngày tạo:</Typography>
                    </Grid>
                    <Grid item xs={8}>

                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>{certificate.createdDate ? formatDate(new Date(certificate.createdDate), "dd/MM/yyyy") : 'Chưa có'}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Ngày cấp:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>
                            {certificate.issuingDate ? formatDate(new Date(certificate.issuingDate), "dd/MM/yyyy") : 'Chưa có'}
                        </Typography>
                    </Grid>


                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Ngày hết hạn:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>
                            {certificate.expirationDate ? formatDate(new Date(certificate.expirationDate), "dd/MM/yyyy") : 'Hiện tại'}
                        </Typography>
                    </Grid>


                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Trạng thái:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Button
                            variant="outlined"
                            color={
                                certificate.requestStatus === 1 ? 'success' :
                                    certificate.requestStatus === 0 ? 'error' :
                                        'warning'
                            }
                            size="medium"
                            sx={{ borderRadius: 2, textTransform: 'none' }}>{statusText(certificate.requestStatus)}</Button>
                    </Grid>

                    <Grid item xs={12} sx={{ display: 'flex', alignItems: 'flex-start' }}>
                        <Grid item xs={4} pr={2}>
                            <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Phản hồi:</Typography>
                        </Grid>
                        <Grid item xs={8}>
                            <TextField
                                value={certificate.feedback || 'Không có phản hồi'}
                                fullWidth
                                multiline
                                readOnly
                                rows={4}
                                variant="outlined"
                                sx={{ backgroundColor: '#f0f0f0', borderRadius: 1 }}
                            />
                        </Grid>
                    </Grid>
                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: '500', textAlign: 'right' }}>Hình ảnh {!certificate?.identityCardNumber ? 'chứng chỉ' : 'căn cước'}:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography sx={{ fontWeight: 'bold', color: '#616161' }}>{certificate?.certificateMedias?.length === 0 ? 'Không có hình ảnh' : `Hiện có ${certificate?.certificateMedias?.length} hình ảnh`}</Typography>
                    </Grid>
                    <Grid item xs={4}>
                    </Grid>
                    <Grid item xs={8}>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                            {certificate?.certificateMedias?.map((image, index) => (
                                <Box
                                    key={index}
                                    sx={{
                                        position: 'relative',
                                        width: 100,
                                        height: 100,
                                        borderRadius: '8px',
                                        overflow: 'hidden',
                                        boxShadow: '0px 2px 10px rgba(0, 0, 0, 0.1)',
                                    }}
                                >
                                    <img src={image.urlPath} alt="Chứng chỉ" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                    <IconButton
                                        sx={{ position: 'absolute', top: "0", left: "0", color: '#fff', backgroundColor: 'rgba(0, 0, 0, 0.5)' }}
                                        size="small"
                                        onClick={() => handleImageClick(image.urlPath)}
                                    >
                                        <VisibilityIcon fontSize="small" />
                                    </IconButton>
                                </Box>
                            ))}
                        </Box>
                    </Grid>
                </Grid>

                <Box sx={{ textAlign: 'right', mt: 4 }}>
                    <Button
                        variant="contained"
                        color="primary"
                        sx={{ width: '150px', height: '45px', fontSize: '16px', textTransform: 'none' }}
                        onClick={() => navigate('/autismtutor/tutor-setting', { state: { selectedTab: '4' } })}
                    >
                        Quay lại
                    </Button>
                </Box>
            </Paper>
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
        </Box>
    );
}

export default CertificateDetail;
