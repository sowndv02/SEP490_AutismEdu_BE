import { Box, Button, Dialog, DialogContent, FormHelperText, Grid, IconButton, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material'
import { useFormik } from 'formik';
import React, { useEffect, useRef, useState } from 'react'
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { enqueueSnackbar } from 'notistack';
import DeleteIcon from '@mui/icons-material/Delete';
import ArrowBackIosIcon from '@mui/icons-material/ArrowBackIos';
import ArrowForwardIosIcon from '@mui/icons-material/ArrowForwardIos';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import services from '~/plugins/services';
function Identification({ tutorInformation, certificate, career, identifcation, setIdentification, activeStep, handleBack, handleNext, steps }) {
    const [citizenIdentification, setCitizenIdentification] = useState(null);
    const [handPhoto, setHandPhoto] = useState(null);
    const [currentImage, setCurrentImage] = useState(null);
    const [currentImage2, setCurrentImage2] = useState(null);
    const cIInput = useRef();
    const [inputKey, setInputKey] = useState(0);
    const [openDialog, setOpenDialog] = useState(false);
    const [openDialog2, setOpenDialog2] = useState(false);

    const validate = values => {
        const errors = {};
        if (!values.issuingInstitution) {
            errors.issuingInstitution = "Bắt buộc"
        }
        if (!values.issuingDate) {
            errors.issuingDate = "Bắt buộc"
        }
        if (!values.identityCardNumber) {
            errors.identityCardNumber = "Bắt buộc"
        }
        if (!citizenIdentification || citizenIdentification?.length === 0) {
            errors.citizenIdentification = "Bắt buộc"
        }
        if (!handPhoto || handPhoto?.length === 0) {
            errors.handPhoto = "Bắt buộc"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            certificateName: 'Căn cước công dân',
            issuingInstitution: identifcation?.issuingInstitution || '',
            issuingDate: identifcation?.issuingDate || '',
            identityCardNumber: identifcation?.identityCardNumber || ''
        },
        validate,
        onSubmit: async (values) => {
            const dataTransfer = new DataTransfer();

            citizenIdentification.forEach(file => {
                dataTransfer.items.add(file);
            });
            handPhoto.forEach(file => {
                dataTransfer.items.add(file);
            });
            const submitData = {
                certificateName: values.certificateName,
                issuingInstitution: values.issuingInstitution,
                issuingDate: values.issuingDate,
                identityCardNumber: values.identityCardNumber,
                medias: [...citizenIdentification, ...handPhoto]
                // citizenIdentification: citizenIdentification,
                // handPhoto: handPhoto
            }
            // setIdentification(submitData)
            const address = `${tutorInformation.province.name}|${tutorInformation.district.name}|${tutorInformation.commune.name}|${tutorInformation.homeNumber}`
            // const apiTutorInformation = {
            //     formalName: tutorInformation.formalName,
            //     phoneNumber: tutorInformation.phoneNumber,
            //     dateOfBirth: tutorInformation.dateOfBirth,
            //     homeNumber: tutorInformation.homeNumber,
            //     startAge: tutorInformation.startAge,
            //     endAge: tutorInformation.endAge,
            //     address: address
            // }
            const apiData = {
                tutorInfo: {
                    formalName: tutorInformation.formalName,
                    dateOfBirth: tutorInformation.dateOfBirth,
                    startAge: tutorInformation.startAge,
                    endAge: tutorInformation.endAge
                },
                tutorBasicInfo: {
                    phoneNumber: tutorInformation.phoneNumber,
                    fullName: tutorInformation.formalName,
                    address: address,
                    image: tutorInformation.avatar
                },
                certificate: [
                    ...certificate,
                    submitData
                ],
                career
            }
            console.log(apiData);
            handleSubmit(apiData)
        }
    });

    const handleSubmit = async (data) => {
        try {
            services.TutorManagementAPI.registerAsTutor(data,
                (res) => {
                    console.log(res);
                    enqueueSnackbar("Đăng ký thành công!")
                }, (error) => {
                    console.log(error);
                    enqueueSnackbar("Đăng ký thất bại!")
                }
            )
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Grid item xs={3} textAlign="right">Số căn cước công dân</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.identityCardNumber}
                            name='identityCardNumber'
                            onChange={formik.handleChange} />
                        {
                            formik.errors.identityCardNumber && (
                                <FormHelperText error>
                                    {formik.errors.identityCardNumber}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Nơi cấp</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.issuingInstitution}
                            name='issuingInstitution'
                            onChange={formik.handleChange} />
                        {
                            formik.errors.issuingInstitution && (
                                <FormHelperText error>
                                    {formik.errors.issuingInstitution}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Ngày cấp</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.issuingDate}
                            name='issuingDate'
                            onChange={formik.handleChange}
                            type='date' />
                        {
                            formik.errors.issuingDate && (
                                <FormHelperText error>
                                    {formik.errors.issuingDate}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Hình ảnh chụp của thẻ CCCD <Typography>(mặt trước và mặt sau)</Typography> </Grid>
                    <Grid item xs={9}>
                        <TextField size='small' type='file' sx={{ width: "70%" }}
                            onChange={(e) => {
                                console.log(e.target.value);
                                if (e.target.files.length > 2) {
                                    enqueueSnackbar("Chỉ chọn 2 ảnh", { variant: "error" });
                                    e.target.value = "";
                                } else {
                                    setCitizenIdentification(Array.from(e.target.files))
                                    setInputKey(preKey => preKey + 1)
                                }
                            }}
                            inputProps={{
                                multiple: true,
                                accept: "image/png, image/jpeg"
                            }}
                            key={inputKey}
                            ref={cIInput}
                        />
                        {
                            citizenIdentification?.length === 0 && (
                                <FormHelperText error>
                                    Bắt buộc
                                </FormHelperText>
                            )
                        }
                        <Stack direction="row" gap={2} flexWrap="wrap">
                            {
                                citizenIdentification && citizenIdentification.map((image, index) => {
                                    return (
                                        <Box mt={2} sx={{
                                            width: '100px', height: "100px", position: "relative",
                                            overflow: "hidden",
                                            ":hover": {
                                                ".overlay-image": {
                                                    width: "100%",
                                                    height: "100%",
                                                    position: 'absolute',
                                                    top: "0",
                                                    left: "0",
                                                    bgcolor: "#676b7b5e",
                                                    display: "flex",
                                                    justifyContent: 'center',
                                                    alignItems: 'center'
                                                }
                                            }
                                        }} key={index}>
                                            <img src={URL.createObjectURL(image)} alt="Preview" style={{ width: '100%', height: "100%" }} />
                                            <Box sx={{ display: "none" }} className="overlay-image">
                                                <RemoveRedEyeIcon sx={{ color: "white", cursor: "pointer" }}
                                                    onClick={() => { setOpenDialog(true), setCurrentImage(index) }} />
                                                <DeleteIcon sx={{ color: "white", cursor: "pointer" }} onClick={() => {
                                                    const fArray = citizenIdentification.filter((img, i) => {
                                                        return i !== index;
                                                    })
                                                    setCitizenIdentification(fArray)
                                                }} />
                                            </Box>
                                        </Box>
                                    )
                                })
                            }
                        </Stack>
                    </Grid>
                    <Grid item xs={3} textAlign="right">Ảnh đang cầm CCCD của bạn</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' type='file' sx={{ width: "70%" }}
                            onChange={(e) => {
                                if (e.target.files.length > 2) {
                                    enqueueSnackbar("Chỉ chọn 2 ảnh", { variant: "error" });
                                    e.target.value = "";
                                } else {
                                    setHandPhoto(Array.from(e.target.files))
                                    setInputKey(preKey => preKey + 1)
                                }
                            }}
                            inputProps={{
                                multiple: true,
                                accept: "image/png, image/jpeg"
                            }}
                            key={inputKey}
                        />
                        {
                            handPhoto?.length === 0 && (
                                <FormHelperText error>
                                    Bắt buộc
                                </FormHelperText>
                            )
                        }
                        <Stack direction="row" gap={2} flexWrap="wrap">
                            {
                                handPhoto && handPhoto.map((image, index) => {
                                    return (
                                        <Box mt={2} sx={{
                                            width: '100px', height: "100px", position: "relative",
                                            overflow: "hidden",
                                            ":hover": {
                                                ".overlay-image": {
                                                    width: "100%",
                                                    height: "100%",
                                                    position: 'absolute',
                                                    top: "0",
                                                    left: "0",
                                                    bgcolor: "#676b7b5e",
                                                    display: "flex",
                                                    justifyContent: 'center',
                                                    alignItems: 'center'
                                                }
                                            }
                                        }} key={index}>
                                            <img src={URL.createObjectURL(image)} alt="Preview" style={{ width: '100%', height: "100%" }} />
                                            <Box sx={{ display: "none" }} className="overlay-image">
                                                <RemoveRedEyeIcon sx={{ color: "white", cursor: "pointer" }}
                                                    onClick={() => { setOpenDialog2(true), setCurrentImage2(index) }} />
                                                <DeleteIcon sx={{ color: "white", cursor: "pointer" }} onClick={() => {
                                                    const fArray = handPhoto.filter((img, i) => {
                                                        return i !== index;
                                                    })
                                                    setHandPhoto(fArray)
                                                }} />
                                            </Box>
                                        </Box>
                                    )
                                })
                            }
                        </Stack>
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
                    <Button type='submit'>
                        {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                    </Button>
                </Box>
            </form>
            {
                currentImage !== null && (
                    <Modal open={openDialog} onClose={() => setOpenDialog(false)}>
                        <Box
                            display="flex"
                            justifyContent="center"
                            alignItems="center"
                            height="100vh"
                            bgcolor="rgba(0, 0, 0, 0.8)"
                            position="relative"
                        >
                            <img
                                src={URL.createObjectURL(citizenIdentification[currentImage])}
                                alt="large"
                                style={{ maxWidth: '90%', maxHeight: '90%' }}
                            />

                            <IconButton
                                onClick={() => setOpenDialog(false)}
                                style={{ position: 'absolute', top: 20, right: 20, color: 'white' }}
                            >
                                <HighlightOffIcon />
                            </IconButton>
                            <IconButton
                                style={{ position: 'absolute', left: 20, color: 'white' }}
                                onClick={() => setCurrentImage(currentImage === 0 ? 0 : currentImage - 1)}
                            >
                                <ArrowBackIosIcon />
                            </IconButton>
                            <IconButton
                                style={{ position: 'absolute', right: 20, color: 'white' }}
                                onClick={() => setCurrentImage(currentImage === citizenIdentification.length - 1 ? currentImage : currentImage + 1)}
                            >
                                <ArrowForwardIosIcon />
                            </IconButton>
                        </Box>
                    </Modal>
                )
            }
            {
                currentImage2 !== null && (
                    <Modal open={openDialog2} onClose={() => setOpenDialog2(false)}>
                        <Box
                            display="flex"
                            justifyContent="center"
                            alignItems="center"
                            height="100vh"
                            bgcolor="rgba(0, 0, 0, 0.8)"
                            position="relative"
                        >
                            <img
                                src={URL.createObjectURL(handPhoto[currentImage2])}
                                alt="large"
                                style={{ maxWidth: '90%', maxHeight: '90%' }}
                            />

                            <IconButton
                                onClick={() => setOpenDialog2(false)}
                                style={{ position: 'absolute', top: 20, right: 20, color: 'white' }}
                            >
                                <HighlightOffIcon />
                            </IconButton>
                            <IconButton
                                style={{ position: 'absolute', left: 20, color: 'white' }}
                                onClick={() => setCurrentImage2(currentImage2 === 0 ? 0 : currentImage2 - 1)}
                            >
                                <ArrowBackIosIcon />
                            </IconButton>
                            <IconButton
                                style={{ position: 'absolute', right: 20, color: 'white' }}
                                onClick={() => setCurrentImage2(currentImage2 === handPhoto.length - 1 ? currentImage2 : currentImage2 + 1)}
                            >
                                <ArrowForwardIosIcon />
                            </IconButton>
                        </Box>
                    </Modal>
                )
            }
        </>
    )
}

export default Identification
