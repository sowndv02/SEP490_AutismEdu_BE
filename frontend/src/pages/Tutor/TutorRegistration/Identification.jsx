import { Box, Button, Dialog, DialogContent, Grid, MenuItem, Select, Stack, TextField, Typography } from '@mui/material'
import { useFormik } from 'formik';
import React, { useEffect, useRef, useState } from 'react'
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { enqueueSnackbar } from 'notistack';
import DeleteIcon from '@mui/icons-material/Delete';
function Identification({ activeStep, handleBack, handleNext, steps }) {
    const [citizenIdentification, setCitizenIdentification] = useState(null);
    const [handPhoto, setHandPhoto] = useState(null);
    const [open, setOpen] = useState(false);
    const [currentImage, setCurrentImage] = useState(null);
    const cIInput = useRef();
    const [inputKey, setInputKey] = useState(0);
    // useEffect(() => {
    //     let uploadedImages = []
    //     if (citizenIdentification) {
    //         const cIPhotos = [];
    //         uploadedImages = [...uploadedImages,]
    //     }
    //     console.log(citizenIdentification);
    // }, [citizenIdentification])

    const handleClose = () => {
        setOpen(false);
    };

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
                <Grid item xs="4" textAlign="right">Số căn cước công dân</Grid>
                <Grid item xs="8"> <TextField size='small' sx={{ width: "50%" }} /></Grid>
                <Grid item xs="4" textAlign="right">Hình ảnh chụp của thẻ CCCD <Typography>(mặt trước và mặt sau)</Typography> </Grid>
                <Grid item xs="8">
                    <TextField size='small' type='file' sx={{ width: "50%" }}
                        onChange={(e) => {
                            console.log(e.target.value);
                            if (e.target.files.length > 2) {
                                enqueueSnackbar("Chỉ chọn 2 ảnh", { variant: "error" });
                                e.target.value = "";
                            } else {
                                setCitizenIdentification(e.target.files)
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
                    <Stack direction="row" gap={2} flexWrap="wrap">
                        {
                            citizenIdentification && Array.from(citizenIdentification).map((image, index) => {
                                return (
                                    <Box mt={2} sx={{ width: '200px', height: "200px", position: "relative", overflow: "hidden" }} key={index}>
                                        <img src={URL.createObjectURL(image)} alt="Preview" style={{ width: '100%', height: "100%" }} />
                                        <Box sx={{
                                            width: "100%",
                                            height: "100%",
                                            position: 'absolute',
                                            top: "0",
                                            left: "0",
                                            bgcolor: "#676b7b5e",
                                            display: "flex",
                                            justifyContent: 'center',
                                            alignItems: 'center'
                                        }}>
                                            <RemoveRedEyeIcon sx={{ color: "white", cursor: "pointer" }} onClick={() => { setOpen(true), setCurrentImage(image) }} />
                                            <DeleteIcon sx={{ color: "white", cursor: "pointer" }} onClick={() => {
                                                const fileList = new DataTransfer();
                                                for (let i = 0; i < citizenIdentification.length; i++) {
                                                    if (i !== index) {
                                                        fileList.items.add(citizenIdentification[i]);
                                                    }
                                                }
                                                cIInput.current.value = `${fileList.length - 1}`
                                                setCitizenIdentification(fileList.files);
                                            }} />
                                        </Box>
                                    </Box>
                                )
                            })
                        }
                    </Stack>
                </Grid>
                <Grid item xs="4" textAlign="right">Ảnh đang cầm CCCD của bạn</Grid>
                <Grid item xs="8"> <TextField size='small' type='file' sx={{ width: "50%" }} /></Grid>
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
            {
                (open && (
                    <Dialog open={open} onClose={handleClose}>
                        <DialogContent style={{ textAlign: 'center' }}>
                            <img src={currentImage ? URL.createObjectURL(currentImage) : ""} style={{ maxHeight: "500px", minHeight: "400px", maxWidth: "100%" }} />
                        </DialogContent>
                    </Dialog>
                ))
            }
        </>
    )
}

export default Identification
