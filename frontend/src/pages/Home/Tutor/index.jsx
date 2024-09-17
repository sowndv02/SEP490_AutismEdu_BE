import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, IconButton, Typography } from '@mui/material'
import React from 'react'
import ChipComponent from '~/components/ChipComponent'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import SubdirectoryArrowLeftIcon from '@mui/icons-material/SubdirectoryArrowLeft';
function Tutor() {

    const formatter = new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0, // Số chữ số thập phân sau dấu chấm
        maximumFractionDigits: 0, // Số chữ số thập phân sau dấu chấm
    });
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center' }}>

            <ChipComponent text="GIA SƯ NỔI BẬT" bgColor="#e4e9fd" color="#2f57f0" />
            <Box textAlign={'center'} sx={{ marginBottom: "50px" }}>
                <Typography variant='h3' sx={{ fontSize: "44px", width: "60%", margin: "auto", color: "#192335", fontWeight: "bold" }}>
                    Tìm Gia Sư Phù Hợp Nhất Cho Con Của Bạn
                </Typography>
            </Box>
            <Grid container mb={5}>
                <Grid item xs={2}></Grid>
                <Grid item xs={8}>
                    <Grid container m={0} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={12} md={7} >
                            <Card sx={{ display: 'flex', p: "30px" }}>
                                <CardMedia
                                    component="img"
                                    sx={{ width: 300 }}
                                    image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-04.webp"
                                    alt="Live from space album cover"
                                />
                                <Box sx={{ display: 'flex', flexDirection: 'column' }}>
                                    <CardContent sx={{ flex: '1 0 auto' }}>
                                        <Typography component="div" variant="h4">
                                            John Alex
                                        </Typography>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                            <LocationOnOutlinedIcon />
                                            <Typography>Hồ Chí Minh</Typography>
                                        </Box>
                                        <Typography mt={2} sx={{
                                            display: '-webkit-box',
                                            WebkitLineClamp: 3,
                                            WebkitBoxOrient: 'vertical',
                                            overflow: 'hidden',
                                            textOverflow: 'ellipsis',
                                        }}>
                                            HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho
                                        </Typography>
                                        <Box sx={{ display: "flex", gap: "20px" }}>
                                            <Box sx={{
                                                display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                '&:hover': {
                                                    color: "blue"
                                                }
                                            }}>
                                                <LocalPhoneOutlinedIcon />
                                                <a href='tel:40404040404'><Typography sx={{
                                                    '&:hover': {
                                                        color: "blue"
                                                    }
                                                }}>40404040404</Typography></a>
                                            </Box>
                                            <Box sx={{
                                                display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                '&:hover': {
                                                    color: "blue"
                                                }
                                            }}>
                                                <EmailOutlinedIcon />
                                                <a href='mailto:xuanthulab.net@gmail.com'><Typography
                                                    sx={{
                                                        '&:hover': {
                                                            color: "blue"
                                                        }
                                                    }}
                                                >abc@gmail.com</Typography></a>
                                            </Box>
                                        </Box>
                                        <Typography mt={5} variant='h5'>{formatter.format(100000)} / buổi</Typography>

                                    </CardContent>
                                    <CardActions>
                                        <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}>Tìm hiểu thêm </Button>
                                    </CardActions>
                                </Box>

                            </Card>
                        </Grid>
                        <Grid item xs={12} md={5} sx={{ height: "510px" }}>
                            <Grid container m={0} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"} sx={{ height: "100%" }}>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1, position: 'relative' }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-04.webp"
                                            alt="green iguana"
                                        />
                                        <Box
                                            sx={{
                                                position: 'absolute',
                                                top: 0,
                                                left: 0,
                                                right: 0,
                                                bottom: 0,
                                                bgcolor: '#0009c933', // Màu đen với opacity 50%
                                                display: 'flex',
                                                justifyContent: 'center',
                                                alignItems: 'center',
                                                transition: 'opacity 0.3s ease',
                                            }}
                                        >
                                            {/* Icon ở giữa */}
                                            <SubdirectoryArrowLeftIcon sx={{ color: 'white', fontSize: 40 }} />
                                        </Box>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1 }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-08.webp"
                                            alt="green iguana"
                                        />
                                    </Card>
                                </Grid>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1 }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-05.webp"
                                            alt="green iguana"
                                        />
                                    </Card>
                                </Grid>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1 }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-07.webp"
                                            alt="green iguana"
                                        />
                                    </Card>
                                </Grid>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1 }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-03.webp"
                                            alt="green iguana"
                                        />
                                    </Card>
                                </Grid>
                                <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                    <Card sx={{ maxWidth: 150, height: "100%", p: 1 }}>
                                        <CardMedia
                                            component="img"
                                            height="100%"
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-02.webp"
                                            alt="green iguana"
                                        />
                                    </Card>
                                </Grid>

                            </Grid>
                        </Grid>
                    </Grid>
                </Grid>
                <Grid item xs={2}></Grid>
            </Grid >
        </Box >
    )
}
export default Tutor
