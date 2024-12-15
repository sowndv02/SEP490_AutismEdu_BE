import ArrowBackIosIcon from '@mui/icons-material/ArrowBackIos';
import ArrowForwardIosIcon from '@mui/icons-material/ArrowForwardIos';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import { Avatar, Box, Grid, IconButton, Modal, Paper, Stack, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { formatter } from '~/utils/service';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import CastForEducationIcon from '@mui/icons-material/CastForEducation';
function BasicInformation({ information, certificates }) {
    const [verification, setVerification] = useState(null);
    const [currentImg, setCurrentImg] = useState(0);
    const [openDialog, setOpenDialog] = React.useState(false);
    useEffect(() => {
        if (information && certificates) {
            const CCCD = certificates.find((c) => {
                return c.certificateName === "Căn cước công dân"
            })
            setVerification(CCCD)
        }
    }, [information, certificates])
    const formatDate = (date) => {
        const dateObj = new Date(date);
        const formattedDate = dateObj.getDate().toString().padStart(2, '0') + '/' +
            (dateObj.getMonth() + 1).toString().padStart(2, '0') + '/' +
            dateObj.getFullYear();
        return formattedDate;
    }

    const formatAddress = (address) => {
        if (!address) return ""
        const addressParts = address?.split('|');
        const formattedAddress = `${addressParts[3]} - ${addressParts[2]} - ${addressParts[1]} - ${addressParts[0]}`;
        return formattedAddress;
    }
    return (
        <Box mt={3}>
            <Stack direction="row" sx={{
                gap: 2
            }}>
                <Paper variant='elevation' sx={{
                    width: "50%",
                    p: 2
                }}>
                    <Stack direction='row' mb={2} gap={2} bgcolor="#F1F8E9" p={1} borderRadius="5px"
                        sx={{
                            border: "1px solid #AED581"
                        }}
                    >
                        <AccountCircleIcon sx={{ color: "#558B2F" }} />
                        <Typography variant='h5' color="#558B2F">Thông tin các nhân</Typography>
                    </Stack>
                    <Avatar src={information?.imageUrl}
                        sx={{
                            width: "100px",
                            height: '100px',
                            margin: "auto",
                            mt: 2
                        }} />
                    <Grid container pl={2} py="50px" columnSpacing={2} rowSpacing={1.5}>
                        <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Họ và tên:</Grid>
                        <Grid item xs={9}>{information?.fullName}</Grid>
                        <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Email:</Grid>
                        <Grid item xs={9}>{information?.email}</Grid>
                        <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Ngày sinh:</Grid>
                        <Grid item xs={9}>{formatDate(information?.dateOfBirth)}</Grid>
                        <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Địa chỉ:</Grid>
                        <Grid item xs={9}>{formatAddress(information?.address)}</Grid>
                        <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Số điện thoại:</Grid>
                        <Grid item xs={9}>{information?.phoneNumber}</Grid>
                        {
                            verification && (
                                <>
                                    <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Mã CCCD:</Grid>
                                    <Grid item xs={9}>{verification?.identityCardNumber}</Grid>
                                    <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Nơi cấp:</Grid>
                                    <Grid item xs={9}>{verification?.issuingInstitution}</Grid>
                                    <Grid item xs={3} textAlign="right" sx={{ fontWeight: "bold", color: "black" }}>Ngày cấp:</Grid>
                                    <Grid item xs={9}>{verification?.issuingDate ? formatDate(verification.issuingDate) : ""}
                                        <Box>
                                            {
                                                verification.certificateMedias?.map((v, index) => {
                                                    return (
                                                        <img key={v.id} src={v.urlPath} alt='cccd' width={70} height={70} style={{
                                                            marginRight: "10px",
                                                            marginTop: "10px",
                                                            cursor: "pointer"
                                                        }}
                                                            onClick={() => { setCurrentImg(index); setOpenDialog(true) }}
                                                        />
                                                    )
                                                })
                                            }
                                        </Box>
                                    </Grid>
                                </>
                            )
                        }
                    </Grid>
                </Paper>
                <Paper variant='elevation' sx={{
                    width: "50%",
                    p: 2
                }}>
                    <Stack direction='row' mb={2} gap={2} bgcolor="#EDE7F6" p={1} borderRadius="5px"
                        sx={{
                            border: "1px solid #B39DDB"
                        }}
                    >
                        <CastForEducationIcon sx={{ color: "#512DA8" }} />
                        <Typography variant='h5' color="#512DA8">Thông tin gia sư</Typography>
                    </Stack>
                    <Typography mt={2}><span style={{ fontWeight: "bold", color: "black" }}>Độ tuổi dạy:</span>
                        <span style={{ marginLeft: "20px" }}>{information?.startAge} - {information?.endAge} tuổi </span></Typography>
                    <Typography mt={2}><span style={{ fontWeight: "bold", color: "black" }}>Số tiếng trên buổi:</span>
                        <span style={{ marginLeft: "20px" }}>{information?.sessionHours} tiếng / buổi</span></Typography>
                    <Typography mt={2}><span style={{ fontWeight: "bold", color: "black" }}>Giá:</span>
                        {
                            information?.priceFrom !== information?.priceEnd ? (
                                <span style={{ marginLeft: "20px" }}>{formatter.format(information?.priceFrom)} - {formatter.format(information?.priceEnd)}</span>
                            ) : (
                                <span style={{ marginLeft: "20px" }}>{formatter.format(information?.priceFrom)}</span>
                            )
                        }
                    </Typography>
                    <Typography mt={2}><span style={{ fontWeight: "bold", color: "black" }}>Giới thiệu:</span> </Typography>
                    <Box sx={{ maxHeight: "420px", mt: 2, overflowY: "auto", p: 3, borderRadius: "5px", border: "1px gray solid" }}>
                        <Box sx={{ maxWidth: "100%" }} dangerouslySetInnerHTML={{ __html: information?.aboutMe }} />
                    </Box>
                </Paper>
            </Stack>
            {
                verification !== null && openDialog && (
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
                                src={verification.certificateMedias[currentImg].urlPath}
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
                                onClick={() => setCurrentImg(currentImg === 0 ? 0 : currentImg - 1)}
                            >
                                <ArrowBackIosIcon />
                            </IconButton>
                            <IconButton
                                style={{ position: 'absolute', right: 20, color: 'white' }}
                                onClick={() => setCurrentImg(currentImg === verification.certificateMedias.length - 1 ? currentImg : currentImg + 1)}
                            >
                                <ArrowForwardIosIcon />
                            </IconButton>
                        </Box>
                    </Modal>
                )
            }
        </Box >
    )
}

export default BasicInformation
