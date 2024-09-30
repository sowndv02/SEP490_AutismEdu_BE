import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, IconButton, Stack, Typography } from '@mui/material'
import React from 'react'
import ChipComponent from '~/components/ChipComponent'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import SubdirectoryArrowLeftIcon from '@mui/icons-material/SubdirectoryArrowLeft';
import { formatter } from '~/utils/service';
import ButtonIcon from '~/components/ButtonComponent/ButtonIcon';
function Tutor() {
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center', bgcolor: "#f9f9ff" }}>

            <ChipComponent text="GIA SƯ NỔI BẬT" bgColor="#e4e9fd" color="#2f57f0" />
            <Box textAlign={'center'} sx={{ marginBottom: "50px" }}>
                <Typography variant='h2' sx={{ width: "60%", margin: "auto", color: "#192335", }}>
                    Tìm Gia Sư Phù Hợp Nhất Cho Con Của Bạn
                </Typography>
            </Box>
            <Stack direction='row' sx={{ justifyContent: "center", width: "100vw" }}>
                <Stack direction="row" sx={{
                    textAlign: "left",
                    width: {
                        xl: "80%",
                        lg: "90%"
                    },
                    gap: "20px"
                }}>
                    <Box sx={{ width: "60%" }} pt={1}>
                        <Card sx={{ display: 'flex', p: "30px", width: "100%", boxSizing: "border-box", alignItems: "center" }}>
                            <Box sx={{ flexBasis: "40%" }}>
                                <img src='https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-04.webp'
                                    style={{ objectFit: "cover", width: "100%", height: "auto" }}
                                    alt='Live from space album cover'
                                />
                            </Box>
                            <Box sx={{ display: 'flex', flexDirection: 'column', flexBasis: "60%" }}>
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
                                    <Box sx={{ display: "flex", gap: "20px", flexWrap: "wrap", mt: 2 }}>
                                        <Box sx={{
                                            display: "flex", alignItems: "center", gap: "10px",
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
                                            display: "flex", alignItems: "center", gap: "10px",
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
                    </Box>
                    <Box sx={{ width: "40%" }} >
                        <Grid container m={0} spacing={{ xs: 2, md: 3 }} textAlign={"left"} sx={{ height: "100%" }}
                            columnSpacing={2} rowSpacing={1}
                        >
                            <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                <Card sx={{ width: "100%", height: "100%", p: 1, position: 'relative', cursor: "pointer" }}>
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
                                <Card sx={{ width: "100%", height: "100%", p: 1 }}>
                                    <CardMedia
                                        component="img"
                                        height="100%"
                                        image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-08.webp"
                                        alt="green iguana"
                                    />
                                </Card>
                            </Grid>
                            <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                <Card sx={{ width: "100%", height: "100%", p: 1 }}>
                                    <CardMedia
                                        component="img"
                                        height="100%"
                                        image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-05.webp"
                                        alt="green iguana"
                                    />
                                </Card>
                            </Grid>
                            <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                <Card sx={{ width: "100%", height: "100%", p: 1 }}>
                                    <CardMedia
                                        component="img"
                                        height="100%"
                                        image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-07.webp"
                                        alt="green iguana"
                                    />
                                </Card>
                            </Grid>
                            <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                <Card sx={{ width: "100%", height: "100%", p: 1 }}>
                                    <CardMedia
                                        component="img"
                                        height="100%"
                                        image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-03.webp"
                                        alt="green iguana"
                                    />
                                </Card>
                            </Grid>
                            <Grid item xs={12} md={4} sx={{ height: "50%" }}>
                                <Card sx={{ width: "100%", height: "100%", p: 1 }}>
                                    <CardMedia
                                        component="img"
                                        height="100%"
                                        image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2024/03/team-02.webp"
                                        alt="green iguana"
                                    />
                                </Card>
                            </Grid>

                        </Grid>
                    </Box>
                </Stack>
            </Stack >
            <Box mt={5} textAlign="center">
                <ButtonIcon text={"XEM THÊM GiA SƯ"} width="400px" height="70px" fontSize="20px" />
            </Box>
        </Box >
    )
}
export default Tutor
