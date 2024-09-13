import { ArrowForward } from '@mui/icons-material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import ClassOutlinedIcon from '@mui/icons-material/ClassOutlined';
import LocalPhoneIcon from '@mui/icons-material/LocalPhone';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import Person3OutlinedIcon from '@mui/icons-material/Person3Outlined';
import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, IconButton, Rating, Typography } from '@mui/material';
import ChipComponent from '~/components/ChipComponent';
function Center() {
    return (
        <Box sx={{ mt: "100px", width: "100vw", bgcolor: "#f9f9ff", py: "100px", textAlign: 'center' }}>

            <ChipComponent text="TRUNG TÂM NỔI BẬT" bgColor="#f1e6fc" color="#b966e7" />
            <Box textAlign={'center'} sx={{ marginBottom: "50px" }}>
                <Typography variant='h3' sx={{ fontSize: "44px", width: "60%", margin: "auto", color: "#192335", fontWeight: "bold" }}>
                    Tìm Trung Tâm Phù Hợp Nhất Cho Con Của Bạn
                </Typography>
            </Box>
            <Grid container mb={5}>
                <Grid item xs={2}></Grid>
                <Grid item xs={8}>
                    <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={8} md={4}>
                            <Card sx={{
                                padding: "20px", minHeight: "670px",
                                transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                '&:hover': {
                                    transform: "scale(1.05) translateY(-10px)",
                                    boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                },
                            }}>
                                <CardMedia
                                    sx={{ height: 240 }}
                                    image="https://touristjourney.com/wp-content/uploads/2020/10/Discover-the-One-Pillar-Pagoda-during-the-Insider-Hanoi-City-Tour-scaled-e1601972144150-1024x569.jpg"
                                    title="Hanoi"
                                />
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <Rating name="read-only" value={3} readOnly />
                                            <Typography ml={2}>(20 reviews)</Typography>
                                        </Box>
                                        <IconButton>
                                            <BookmarkBorderIcon />
                                        </IconButton>
                                    </Box>
                                    <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>
                                        Trung tâm trẻ tự kỷ Inova
                                    </Typography>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "20px" }}>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <ClassOutlinedIcon />
                                            <Typography><span>20 lớp</span></Typography>
                                        </Box>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <Person3OutlinedIcon />
                                            <Typography><span>20 giáo viên</span></Typography>
                                        </Box>
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
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocationOnIcon />
                                        <Typography>Tỉnh/Thành phố: Hồ Chí Minh</Typography>
                                    </Box>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocalPhoneIcon />
                                        <Typography>SĐT: 40404040404</Typography>
                                    </Box>
                                </CardContent>
                                <CardActions>
                                    <Button sx={{ fontSize: "20px" }}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                </CardActions>
                            </Card>
                        </Grid>
                        <Grid item xs={8} md={4}>
                            <Card sx={{
                                padding: "20px", minHeight: "670px",
                                transition: "transform 0.3s ease, box-shadow 0.3s ease", // Tạo hiệu ứng mượt
                                '&:hover': {
                                    transform: "scale(1.05) translateY(-10px)", // Tăng kích thước 5% và nhỉnh lên 10px
                                    boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)", // Thêm bóng đổ khi hover
                                },
                            }}>
                                <CardMedia
                                    sx={{ height: 240 }}
                                    image="https://touristjourney.com/wp-content/uploads/2020/10/Discover-the-One-Pillar-Pagoda-during-the-Insider-Hanoi-City-Tour-scaled-e1601972144150-1024x569.jpg"
                                    title="Hanoi"
                                />
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <Rating name="read-only" value={3} readOnly />
                                            <Typography ml={2}>(20 reviews)</Typography>
                                        </Box>
                                        <IconButton>
                                            <BookmarkBorderIcon />
                                        </IconButton>
                                    </Box>
                                    <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>
                                        Trung tâm trẻ tự kỷ Inova
                                    </Typography>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "20px" }}>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <ClassOutlinedIcon />
                                            <Typography><span>20 lớp</span></Typography>
                                        </Box>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <Person3OutlinedIcon />
                                            <Typography><span>20 giáo viên</span></Typography>
                                        </Box>
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
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocationOnIcon />
                                        <Typography>Tỉnh/Thành phố: Hồ Chí Minh</Typography>
                                    </Box>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocalPhoneIcon />
                                        <Typography>SĐT: 40404040404</Typography>
                                    </Box>
                                </CardContent>
                                <CardActions>
                                    <Button sx={{ fontSize: "20px" }}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                </CardActions>
                            </Card>
                        </Grid>
                        <Grid item xs={8} md={4}>
                            <Card sx={{
                                padding: "20px", minHeight: "670px",
                                transition: "transform 0.3s ease, box-shadow 0.3s ease", // Tạo hiệu ứng mượt
                                '&:hover': {
                                    transform: "scale(1.05) translateY(-10px)", // Tăng kích thước 5% và nhỉnh lên 10px
                                    boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)", // Thêm bóng đổ khi hover
                                },
                            }}>
                                <CardMedia
                                    sx={{ height: 240 }}
                                    image="https://touristjourney.com/wp-content/uploads/2020/10/Discover-the-One-Pillar-Pagoda-during-the-Insider-Hanoi-City-Tour-scaled-e1601972144150-1024x569.jpg"
                                    title="Hanoi"
                                />
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <Rating name="read-only" value={3} readOnly />
                                            <Typography ml={2}>(20 reviews)</Typography>
                                        </Box>
                                        <IconButton>
                                            <BookmarkBorderIcon />
                                        </IconButton>
                                    </Box>
                                    <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>
                                        Trung tâm trẻ tự kỷ Inova
                                    </Typography>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "20px" }}>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <ClassOutlinedIcon />
                                            <Typography><span>20 lớp</span></Typography>
                                        </Box>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                            <Person3OutlinedIcon />
                                            <Typography><span>20 giáo viên</span></Typography>
                                        </Box>
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
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocationOnIcon />
                                        <Typography>Tỉnh/Thành phố: Hồ Chí Minh</Typography>
                                    </Box>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                        <LocalPhoneIcon />
                                        <Typography>SĐT: 40404040404</Typography>
                                    </Box>
                                </CardContent>
                                <CardActions>
                                    <Button sx={{ fontSize: "20px" }}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                </CardActions>
                            </Card>
                        </Grid>
                    </Grid>
                </Grid>
            </Grid>
            <Grid item xs={2}></Grid>
            <Button
                sx={{
                    height: "70px",
                    lineHeight: "70px",
                    padding: "0 35px",
                    color: "white",
                    fontSize: "20px",
                    backgroundImage: 'linear-gradient(to right, #2f57ef, #b966e7, #b966e7, #2f57ef)',
                    backgroundSize: "300% 100%",
                    backgroundPosition: "0% 50%",
                    transition: "background-position 0.3s ease-in-out",
                    '&:hover': {
                        backgroundPosition: "100% 50%",
                        '.icon-start': {
                            opacity: 1,
                            transform: "translateX(0)",
                            transitionDelay: "0.225s"
                        },
                        '.icon-end': {
                            opacity: 0,
                            transform: 'translateX(20px)',
                            visibility: 'hidden',
                        },
                        '.btn-text': {
                            transform: "translateX(23px)",
                            transitionDelay: "0.1s"
                        }
                    },
                }}
            >
                <Typography
                    className='btn-text'
                    variant="span"
                    sx={{
                        transition: "transform 0.6s 0.125s cubic-bezier(0.1, 0.75, 0.25, 1)",
                        marginInlineStart: "-23px",
                    }}
                >
                    Xem Thêm Trung Tâm
                </Typography>
                <ArrowForward className="icon-start"
                    sx={{
                        marginInlineStart: 0,
                        marginInlineEnd: 0, // Sửa tên thuộc tính
                        opacity: 0,
                        transform: "translateX(-10px)",
                        transition: "opacity 0.3s ease, transform 0.3s ease", // Thêm thuộc tính transition
                        transitionDelay: "0s",
                        order: -2
                    }} />
                <ArrowForward className="icon-end"
                    variant="span"
                    sx={{
                        transition: "opacity 0.4s 0.25s, transform 0.6s 0.25s",
                        transitionTimingFunction: "cubic-bezier(0.1, 0.75, 0.25, 1)"
                    }} />
            </Button>
        </Box >
    )
}

export default Center
