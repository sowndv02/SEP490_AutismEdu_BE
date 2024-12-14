import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, List, ListItem, ListSubheader, Stack, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useRef, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import axios from "~/plugins/axiosConfig";
import services from '~/plugins/services';
import Career from './Career';
import CareerDetail from './Career/CareerDetail';
import CertificateAddition from './Certificate/CertificateAddition';
import CertificateDetail from './Certificate/CertificateDetail';
import Cookies from 'js-cookie';
function WorkInfo({ activeStep, handleBack, handleNext, steps, certificate, career, setCareer,
    setCertificate, tutorInformation, tutorIntroduction,
    IdVerification }) {
    const image = useRef(null);
    const [open, setOpen] = useState(false);
    const [openConfirm, setOpenConfirm] = useState(false);
    const handleClose = () => {
        setOpen(false);
    };
    const [loading, setLoading] = useState(false);
    const handleSubmit = async () => {
        try {
            setOpenConfirm(false);
            if (certificate?.length !== 0 && career?.length !== 0) {
                setLoading(true);
                const submitForm = new FormData();
                submitForm.append("Email", tutorInformation.email);
                submitForm.append("FullName", tutorInformation.fullName);
                submitForm.append("PhoneNumber", tutorInformation.phoneNumber);
                submitForm.append("Address", `${tutorInformation.province.name}|${tutorInformation.district.name}|${tutorInformation.commune.name}|${tutorInformation.homeNumber}`);
                submitForm.append("Image", tutorInformation.image);
                submitForm.append("DateOfBirth", tutorInformation.dateOfBirth);
                submitForm.append("StartAge", tutorIntroduction.startAge);
                submitForm.append("EndAge", tutorIntroduction.endAge);
                submitForm.append("PriceFrom", tutorIntroduction.priceFrom);
                submitForm.append("PriceEnd", tutorIntroduction.priceEnd);
                submitForm.append("AboutMe", tutorIntroduction.description);
                submitForm.append("SessionHours", tutorIntroduction.sessionHours);
                tutorIntroduction.curriculum.forEach((curriculum, index) => {
                    submitForm.append(`Curriculums[${index}].Name`, curriculum.name);
                    submitForm.append(`Curriculums[${index}].ageFrom`, curriculum.ageFrom);
                    submitForm.append(`Curriculums[${index}].ageEnd`, curriculum.ageEnd);
                    submitForm.append(`Curriculums[${index}].Description`, curriculum.description);
                });
                career.forEach((experience, index) => {
                    submitForm.append(`WorkExperiences[${index}].CompanyName`, experience.companyName);
                    submitForm.append(`WorkExperiences[${index}].Position`, experience.position);
                    submitForm.append(`WorkExperiences[${index}].StartDate`, experience.startDate);
                    if (experience.endDate !== null) {
                        submitForm.append(`WorkExperiences[${index}].EndDate`, experience.endDate);
                    } else {
                        submitForm.append(`WorkExperiences[${index}].EndDate`, "");
                    }
                });
                certificate.forEach((cert, index) => {
                    submitForm.append(`Certificates[${index}].CertificateName`, cert.certificateName);
                    submitForm.append(`Certificates[${index}].IssuingInstitution`, cert.issuingInstitution);
                    submitForm.append(`Certificates[${index}].IssuingDate`, cert.issuingDate);
                    if (cert.expirationDate) {
                        submitForm.append(`Certificates[${index}].ExpirationDate`, cert.expirationDate);
                    } else {
                        submitForm.append(`Certificates[${index}].ExpirationDate`, "");
                    }
                    Array.from(cert.medias).forEach((file) => {
                        submitForm.append(`Certificates[${index}].Medias`, file);
                    });
                });

                submitForm.append(`Certificates[${certificate.length}].CertificateName`, IdVerification.certificateName);
                submitForm.append(`Certificates[${certificate.length}].issuingInstitution`, IdVerification.issuingInstitution);
                submitForm.append(`Certificates[${certificate.length}].issuingDate`, IdVerification.issuingDate);
                submitForm.append(`Certificates[${certificate.length}].identityCardNumber`, IdVerification.identityCardNumber);
                Array.from(IdVerification.medias).forEach((file) => {
                    submitForm.append(`Certificates[${certificate.length}].Medias`, file);
                });
                axios.setHeaders({ "Content-Type": "multipart/form-data", "Accept": "application/json, text/plain, multipart/form-data, */*" });
                await services.TutorManagementAPI.registerAsTutor(submitForm, (res) => {
                    handleNext();
                }, (err) => {
                    enqueueSnackbar(err.error[0], { variant: "error" })
                })
                Cookies.remove("draftData");
                setLoading(false);
                axios.setHeaders({ "Content-Type": "application/json", "Accept": "application/json, text/plain, */*" });
            } else {
                enqueueSnackbar("Bạn chưa có bằng cấp hoặc kinh nghiệm làm việc", { variant: "error" })
            }
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống, đăng ký thất bại!", { variant: "error" })
            setLoading(false)
        }
    }
    return (
        <>
            <Box>
                <Box>
                    <Typography
                        mt={2}
                        style={{
                            backgroundColor: "#fff4e6",
                            border: "1px solid #ffa502",
                            color: "#d62828",
                            fontWeight: "bold",
                            fontSize: "16px",
                            display: "flex",
                            alignItems: "center",
                            padding: "10px",
                            borderRadius: "8px",
                            gap: "8px"
                        }}
                    >
                        <span style={{ fontSize: "20px" }}>⚠️</span>
                        <i>
                            Bạn chỉ có thể đăng ký tối đa
                            <b style={{ color: "red" }}> 5 bằng cấp/chứng chỉ</b>
                            và
                            <b style={{ color: "red" }}> 5 kinh nghiệm làm việc</b>. Bạn có thể thêm những bằng cấp/chứng chỉ hoặc kinh nghiệm làm việc khác khi được hệ thống phê duyệt!
                        </i>
                    </Typography>
                    <List
                        sx={{ maxWidth: 450, bgcolor: 'background.paper', mt: 3 }}
                        component="nav"
                        aria-labelledby="nested-list-subheader"
                        subheader={
                            <ListSubheader component="div" id="nested-list-subheader">
                                <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                    <Typography variant='h6'>Thêm bằng cấp hoặc chứng chỉ</Typography>
                                    {
                                        certificate && certificate.length < 5 && (
                                            <CertificateAddition certificate={certificate} setCertificate={setCertificate} />
                                        )
                                    }
                                </Stack>
                            </ListSubheader>
                        }
                    >
                        {
                            certificate === null || certificate.length === 0 ? (
                                <ListItem>Chưa có bằng cấp hay chứng chỉ nào</ListItem>
                            ) : (
                                certificate?.map((c, index) => {
                                    return (
                                        <CertificateDetail key={index} index={index} currentItem={c} certificate={certificate}
                                            setCertificate={setCertificate} />
                                    )
                                })
                            )
                        }
                    </List>
                </Box>
                <Box>
                    <List
                        sx={{ maxWidth: 450, bgcolor: 'background.paper' }}
                        component="nav"
                        aria-labelledby="nested-list-subheader"
                        subheader={
                            <ListSubheader component="div" id="nested-list-subheader">
                                <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                    <Typography variant='h6'>Thêm kinh nghiệm làm việc</Typography>
                                    {
                                        career && career.length < 5 && (
                                            <Career career={career} setCareer={setCareer} />
                                        )
                                    }
                                </Stack>
                            </ListSubheader>
                        }
                    >
                        {
                            career === null || career.length === 0 ? (
                                <ListItem>Chưa có kinh nghiệm làm việc nào</ListItem>
                            ) : (

                                career?.map((c, index) => {
                                    return (
                                        <CareerDetail key={index} currentItem={c} career={career} setCareer={setCareer}
                                            index={index} />
                                    )
                                })
                            )
                        }
                    </List>
                </Box>
            </Box>
            <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                <Button
                    color="inherit"
                    disabled={activeStep === 0}
                    onClick={handleBack}
                    sx={{ mr: 1 }}
                >
                    Quay lại
                </Button>
                <Box sx={{ flex: '1 1 auto' }} />
                <Button onClick={() => setOpenConfirm(true)}>
                    {activeStep === steps.length - 1 ? 'Kết thúc' : 'Tiếp theo'}
                </Button>
            </Box>
            {
                image && (
                    <Dialog open={open} onClose={handleClose}>
                        <DialogContent style={{ textAlign: 'center' }}>
                            <img src={image.current?.src} style={{ maxHeight: "500px", minHeight: "400px", maxWidth: "100%" }} />
                        </DialogContent>
                    </Dialog>
                )
            }
            <Dialog
                open={openConfirm}
                onClose={() => setOpenConfirm(false)}
            >
                <DialogTitle id="alert-dialog-title">
                    <Typography variant='h6'>Kiểm tra lại toàn bộ thông khi gửi!</Typography>
                    <Typography>Bạn có muốn nộp đơn này không?</Typography>
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleSubmit}>Nộp</Button>
                    <Button onClick={() => { setOpenConfirm(false) }} autoFocus>
                        Huỷ bỏ
                    </Button>
                </DialogActions>
            </Dialog>
            <LoadingComponent open={loading} setOpen={setLoading} />
        </>
    )
}

export default WorkInfo
