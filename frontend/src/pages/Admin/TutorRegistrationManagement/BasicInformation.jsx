import { Avatar, Box, Grid, IconButton, Modal, Typography } from '@mui/material'
import React, { useState } from 'react'
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
function BasicInformation() {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    return (
        <>
            <IconButton onClick={handleOpen}>
                <RemoveRedEyeIcon />
            </IconButton>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 600,
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    p: 4
                }}>
                    <Avatar src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSjPtBPtOIj16drcpc4Ht93MyJgtRH89ikp_Q&s'
                        sx={{
                            width: "100px",
                            height: '100px',
                            margin: "auto"
                        }} />
                    <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                        <Grid item xs={4} textAlign="right">Họ và tên:</Grid>
                        <Grid item xs={8}>Nguyễn Văn A</Grid>
                        <Grid item xs={4} textAlign="right">Ngày sinh:</Grid>
                        <Grid item xs={8}>09-03-2002</Grid>
                        <Grid item xs={4} textAlign="right">Địa chỉ:</Grid>
                        <Grid item xs={8}>Số 10 - thôn 3 - Thạch Hoà - Thạch Thất - Hà Nội</Grid>
                        <Grid item xs={4} textAlign="right">Số điện thoại:</Grid>
                        <Grid item xs={8}>09384847474</Grid>
                        <Grid item xs={4} textAlign="right">Độ tuổi dạy:</Grid>
                        <Grid item xs={8}>1 - 5 tuổi</Grid>
                    </Grid>
                </Box>
            </Modal>
        </>
    )
}

export default BasicInformation
