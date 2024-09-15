import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Box, Button, Card, CardActions, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material';
import ButtonIcon from '~/components/ButtonComponent/ButtonIcon';
function Blog() {
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center', bgcolor: "#b08fd8" }}>
            <Stack direction='row' sx={{ justifyContent: "center", width: "100vw" }}>
                <Box sx={{
                    width: {
                        xl: "70%",
                        lg: "90%"
                    }
                }}>
                    <Grid container m={0} mb={5} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={12} md={6} >
                            <Chip
                                label={"BÀI ĐĂNG"}
                                sx={{
                                    fontSize: '14px',
                                    padding: '20px 10px',
                                    bgcolor: "#f5e7f1",
                                    fontWeight: "bold",
                                    color: "#DB7093"
                                }}
                            />
                            <Typography variant='h3' sx={{ fontSize: "44px", color: "#192335", fontWeight: "bold" }} mt={3}>
                                Bài Viết Mới Nhất.
                            </Typography>
                        </Grid>
                        <Grid item xs={12} md={6} textAlign={"right"}>
                            <ButtonIcon text={"ĐỌC THÊM"} />
                        </Grid>
                    </Grid>
                    <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={12} md={6} sx={{ height: "500px" }}>
                            <Card sx={{
                                height: "100%",
                                paddingBottom: '20px',
                                transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                '&:hover': {
                                    transform: "scale(1.02) translateY(-10px)",
                                    boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                    cursor: "pointer"
                                }
                            }}>
                                <CardMedia
                                    component="img"
                                    sx={{ height: "60%" }}
                                    image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2023/12/girl-looking-laptop-1.webp"
                                    alt="Live from space album cover"
                                />
                                <Box sx={{ display: 'flex', flexDirection: 'column', height: "40%" }}>
                                    <CardContent sx={{ flex: '1 0 auto' }}>
                                        <Typography component="div" variant="h4">
                                            Difficult Things About Education.
                                        </Typography>
                                        <Typography mt={2} sx={{
                                            display: '-webkit-box',
                                            WebkitLineClamp: 1,
                                            WebkitBoxOrient: 'vertical',
                                            overflow: 'hidden',
                                            textOverflow: 'ellipsis',
                                        }}>
                                            HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho
                                        </Typography>
                                    </CardContent>
                                    <CardActions>
                                        <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}>Tìm hiểu thêm </Button>
                                    </CardActions>
                                </Box>

                            </Card>
                        </Grid>
                        <Grid item xs={12} md={6}>
                            <Grid container m={0} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"} sx={{ height: "100%" }}>
                                <Grid item xs={12} sx={{ height: "33%" }}>
                                    <Card sx={{
                                        display: 'flex', height: "100%",
                                        transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                        '&:hover': {
                                            cursor: "pointer",
                                            transform: "scale(1.02) translateY(-10px)",
                                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                        }
                                    }}>
                                        <CardMedia
                                            component="img"
                                            sx={{ width: 151 }}
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2023/12/image-1-1-150x150.webp"
                                            alt="Live from space album cover"
                                        />
                                        <Box sx={{ height: "100%", display: "flex", alignItems: "center" }}>
                                            <CardContent>
                                                <Typography component="div" variant="h5">
                                                    Live From Space
                                                </Typography>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}>Tìm hiểu thêm </Button>
                                            </CardContent>
                                        </Box>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} sx={{ height: "33%" }}>
                                    <Card sx={{
                                        display: 'flex', height: "100%",
                                        transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                        '&:hover': {
                                            cursor: "pointer",
                                            transform: "scale(1.02) translateY(-10px)",
                                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                        }
                                    }}>
                                        <CardMedia
                                            component="img"
                                            sx={{ width: 151 }}
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2023/12/image-1-1-150x150.webp"
                                            alt="Live from space album cover"
                                        />
                                        <Box sx={{ height: "100%", display: "flex", alignItems: "center" }}>
                                            <CardContent>
                                                <Typography component="div" variant="h5">
                                                    Live From Space
                                                </Typography>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}>Tìm hiểu thêm </Button>
                                            </CardContent>
                                        </Box>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} sx={{ height: "33%" }}>
                                    <Card sx={{
                                        display: 'flex', height: "100%",
                                        transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                        '&:hover': {
                                            transform: "scale(1.02) translateY(-10px)",
                                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                            cursor: "pointer"
                                        }
                                    }}>
                                        <CardMedia
                                            component="img"
                                            sx={{ width: 151 }}
                                            image="https://rainbowthemes.net/themes/histudy/wp-content/uploads/2023/12/image-1-1-150x150.webp"
                                            alt="Live from space album cover"
                                        />
                                        <Box sx={{ height: "100%", display: "flex", alignItems: "center" }}>
                                            <CardContent>
                                                <Typography component="div" variant="h5">
                                                    Live From Space
                                                </Typography>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}>Tìm hiểu thêm </Button>
                                            </CardContent>
                                        </Box>
                                    </Card>
                                </Grid>

                            </Grid>
                        </Grid>
                    </Grid>
                </Box>
            </Stack >
        </Box >
    )
}
export default Blog
