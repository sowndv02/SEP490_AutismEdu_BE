import { Box, Button, Card, CardContent, CardMedia, Grid, Stack, Typography } from '@mui/material';
import ChipComponent from '~/components/ChipComponent';


function BigCity() {
    return (
        <Stack direction="row" sx={{ mt: "30px", paddingBottom: "30px", justifyContent: "center" }}>
            <Box sx={{
                width: {
                    xl: "80%",
                    lg: "90%"
                },
            }}>
                <ChipComponent text="THÀNH PHỐ LỚN" bgColor="#e4e9fd" color="#2f57ef" />
                <Box textAlign={'center'} sx={{ marginBottom: "50px" }}>
                    <Typography variant='h2' sx={{
                        width: "70%", margin: "auto", color: "#192335", fontWeight: "bold"
                    }}>
                        Tìm Trung Tâm Và Gia Sư Ở Các Thành Phố Lớn
                    </Typography>
                </Box>
                <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }}>
                    <Grid item xs={8} md={3}>
                        <Card sx={{
                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                            '&:hover': {
                                transform: "scale(1.05) translateY(-10px)",
                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                            }
                        }}>
                            <CardMedia
                                sx={{ height: 240 }}
                                image="https://touristjourney.com/wp-content/uploads/2020/10/Discover-the-One-Pillar-Pagoda-during-the-Insider-Hanoi-City-Tour-scaled-e1601972144150-1024x569.jpg"
                                title="Hanoi"
                            />
                            <CardContent>
                                <Typography gutterBottom variant="h5" component="div">
                                    Hà Nội
                                </Typography>
                                <Button sx={{ fontWeight: "bold" }}>Khám phá</Button>
                            </CardContent>
                        </Card>
                    </Grid>
                    <Grid item xs={8} md={3}>
                        <Card sx={{
                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                            '&:hover': {
                                transform: "scale(1.05) translateY(-10px)",
                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                            }
                        }}>
                            <CardMedia
                                sx={{ height: 240 }}
                                image="https://tourdanangcity.vn/wp-content/uploads/2022/03/cau-rong-da-nang-7.jpg.jpg"
                                title="Danang"
                            />
                            <CardContent>
                                <Typography gutterBottom variant="h5" component="div">
                                    Đà Nẵng
                                </Typography>
                                <Button sx={{ fontWeight: "bold" }}>Khám phá</Button>
                            </CardContent>
                        </Card>
                    </Grid>
                    <Grid item xs={8} md={3}>
                        <Card sx={{
                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                            '&:hover': {
                                transform: "scale(1.05) translateY(-10px)",
                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                            }
                        }}>
                            <CardMedia
                                sx={{ height: 240 }}
                                image="https://upload.wikimedia.org/wikipedia/commons/f/f6/Ho_Chi_Minh_City_Skyline_%28night%29.jpg"
                                title="Hochiminh"
                            />
                            <CardContent>
                                <Typography gutterBottom variant="h5" component="div">
                                    Hồ Chí Minh
                                </Typography>
                                <Button sx={{ fontWeight: "bold" }}>Khám phá</Button>
                            </CardContent>
                        </Card>
                    </Grid >
                    <Grid item xs={8} md={3}>
                        <Card sx={{
                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                            '&:hover': {
                                transform: "scale(1.05) translateY(-10px)",
                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                            }
                        }}>
                            <CardMedia
                                sx={{ height: 240 }}
                                image="https://i0.wp.com/heza.gov.vn/wp-content/uploads/2023/10/hai_phong-scaled.jpg?fit=2560%2C1662&ssl=1"
                                title="H"
                            />
                            <CardContent>
                                <Typography gutterBottom variant="h5" component="div">
                                    Hải Phòng
                                </Typography>
                                <Button sx={{ fontWeight: "bold" }}>Khám phá</Button>
                            </CardContent>
                        </Card>
                    </Grid>
                </Grid>
            </Box>
        </Stack>
    )
}

export default BigCity
